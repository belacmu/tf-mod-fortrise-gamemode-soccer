using System;
using System.Reflection;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le terrain vu de dessus : plus de chute, plus d'accroupissement, quatre directions.
  ///
  /// <b>Le levier est <c>Player.OnGround</c>, et il n'en fallait pas d'autre.</b> Dans
  /// <c>NormalUpdate</c>, toute la gravite tient dans une seule branche : <c>if
  /// (OnGround) { ... } else { ... Speed.Y = Approach(Speed.Y, chute, g) ... }</c>. Un
  /// archer que le jeu croit pose au sol n'est donc jamais tire vers le bas - ni chute,
  /// ni glissade contre un mur, ni prise de rebord, qui vivent tous dans la meme
  /// branche. On ne desactive pas la gravite : on se place du cote ou elle ne s'applique
  /// pas, ce qui est le seul moyen de ne pas se battre contre elle a chaque image.
  ///
  /// <b>La verticale devient une vitesse, comme l'horizontale.</b> On ecrit
  /// <c>Speed.Y</c> a partir de l'intention du joueur, apres l'update : l'archer avance
  /// vers le haut ou le bas exactement comme il avance a gauche ou a droite. Cela
  /// annule aussi le saut sans avoir a le desactiver - une impulsion ecrasee a l'image
  /// suivante ne va nulle part - et c'etait necessaire : sans gravite, un saut
  /// n'aurait jamais retombe.
  ///
  /// <b>Un archer pose au sol qui pousse vers le bas S'ACCROUPIT</b>, et c'est la seule
  /// chose que le levier fait de travers. <c>InputDucking</c> vaut exactement "au sol,
  /// sans direction horizontale, vers le bas" - c'est-a-dire la moitie de ce qu'on veut
  /// pouvoir faire. On intercepte donc le seul endroit qui le decide : la valeur que
  /// <c>NormalUpdate</c> rend a la machine a etats.
  ///
  /// <b>Tout se passe autour de <c>NormalUpdate</c>, et l'instant est le sujet.</b> Un
  /// correctif pose apres <c>Player.Update</c> n'aurait servi a rien : <c>Update</c>
  /// recalcule <c>OnGround</c> depuis <c>GetBelow()</c> AVANT de faire tourner la
  /// machine a etats, si bien que la valeur ecrite a la fin d'une image serait effacee
  /// au debut de la suivante, avant que quiconque l'ait lue. On se place donc juste
  /// devant le seul consommateur qui compte.
  ///
  /// Ce qui n'est PAS touche : l'esquive et les autres etats, qui ne passent pas par
  /// <c>NormalUpdate</c> - l'esquive n'a de toute facon jamais eu de gravite, et un
  /// archer gele avant l'engagement doit rester gele.
  /// </summary>
  public class FlatPlay : IHookable
  {
    /// <summary>
    /// Vitesse de deplacement vertical, en pixels par image.
    ///
    /// La meme que la course horizontale du jeu (<c>MaxRunSpeed</c> vaut 1,5) : sur un
    /// plateau vu de dessus, il n'y a pas de raison d'aller moins vite vers le haut que
    /// vers le cote, et une difference se sentirait tout de suite comme un defaut.
    /// </summary>
    private const float SPEED = 1.5f;

    /// <summary>Vitesse initiale du saut sur place, en pixels par image.</summary>
    private const float HOP_SPEED = 2.6f;

    /// <summary>Ce que le saut sur place perd par image. Il decide de sa duree.</summary>
    private const float HOP_GRAVITY = 0.16f;

    /// <summary>Etat "accroupi" de la machine a etats du joueur.</summary>
    private const int DUCKING = 2;

    /// <summary>Etat "normal".</summary>
    private const int NORMAL = 0;

    /// <summary>
    /// Ecriture de <c>Player.OnGround</c>, dont l'accesseur est prive.
    ///
    /// Un DELEGUE et non un <c>MethodInfo</c> qu'on invoque : l'appel a lieu a chaque
    /// image et pour chaque archer, et <c>Invoke</c> alloue un tableau d'arguments et
    /// emballe le booleen a chaque fois. La forme ouverte du delegue - l'instance en
    /// premier parametre - evite les deux.
    /// </summary>
    private static Action<Player, bool> setGround;

    private static FieldInfo moveAxis;

    /// <summary>L'etat d'entree que le joueur a deja lu cette image.</summary>
    private static FieldInfo inputState;

    /// <summary>Hauteur de depart du saut, par archer. NaN : pas de saut en cours.</summary>
    private static readonly float[] hopFloor = new float[8];

    /// <summary>Vitesse verticale du saut en cours, par archer.</summary>
    private static readonly float[] hopSpeed = new float[8];

    private static readonly bool[] hopping = new bool[8];

    /// <summary>
    /// Cet archer est-il en l'air ?
    ///
    /// Le ballon le demande : sauter par-dessus un ballon qui roule est TOUT l'interet
    /// du saut sur un terrain plat. Sans cela le saut ne serait qu'une animation.
    /// </summary>
    public static bool Hopping(int playerIndex)
    {
      return playerIndex >= 0 && playerIndex < hopping.Length && hopping[playerIndex];
    }

    /// <summary>Efface les sauts en cours. A appeler au DEBUT de chaque manche.</summary>
    public static void Reset()
    {
      // Monocle n'appelle ni Removed ni ne vide Scene en fin de manche : un archer qui
      // finissait la manche en l'air resterait "en saut" a la manche suivante, et
      // traverserait le ballon sans jamais pouvoir le prendre.
      for (int i = 0; i < hopping.Length; i++)
      {
        hopping[i] = false;
        hopSpeed[i] = 0f;
      }
    }

    public static void Load(IHarmony harmony)
    {
      MethodInfo setter = AccessTools.PropertySetter(typeof(Player), "OnGround");
      moveAxis = AccessTools.Field(typeof(Player), "moveAxis");
      inputState = AccessTools.Field(typeof(Player), "input");

      // La visee libre, sans toucher aux variantes du joueur.
      //
      // Le jeu decide de l'arrondi a huit directions en passant "snap" a
      // GetAimDirection, calcule depuis la variante FreeAiming. On force ce seul
      // parametre plutot que d'ecrire dans les variantes : celles-ci appartiennent au
      // joueur, elles sont enregistrees avec ses reglages de match, et un mode n'a pas a
      // les changer dans son dos pour tous les autres modes.
      MethodInfo aim = AccessTools.DeclaredMethod(typeof(PlayerInput),
          nameof(PlayerInput.GetAimDirection));

      if (aim != null)
      {
        harmony.Patch(aim,
            prefix: new HarmonyMethod(typeof(FlatPlay), nameof(GetAimDirection_prefix)));
      }

      if (setter != null)
      {
        try
        {
          setGround = (Action<Player, bool>)setter.CreateDelegate(typeof(Action<Player, bool>));
        }
        catch (Exception)
        {
          // Fabriquer un delegue sur un accesseur prive n'est pas garanti par toutes
          // les executions. On retombe alors sur l'invocation ordinaire : elle alloue
          // a chaque image, ce qui est le prix a payer pour que le mode marche partout
          // plutot que de ne pas marcher du tout.
          setGround = (player, value) => setter.Invoke(player, [value]);
        }
      }

      MethodInfo normal = AccessTools.DeclaredMethod(typeof(Player), "NormalUpdate");

      if (setGround == null || moveAxis == null || normal == null)
      {
        Logger.Error("[Flat] Player.OnGround, moveAxis ou NormalUpdate introuvable : "
            + "le terrain plat restera sans effet");
        return;
      }

      harmony.Patch(normal,
          prefix: new HarmonyMethod(typeof(FlatPlay), nameof(NormalUpdate_prefix)),
          postfix: new HarmonyMethod(typeof(FlatPlay), nameof(NormalUpdate_postfix)));
    }

    /// <summary>
    /// Pose l'archer "au sol" et lui donne sa vitesse verticale, juste avant que le jeu
    /// ne s'en serve.
    ///
    /// L'ordre est tout : <c>Update</c> renseigne <c>OnGround</c> depuis le decor, PUIS
    /// fait tourner la machine a etats qui appelle cette methode. Ecrire ici est donc
    /// le seul moment ou la valeur survit jusqu'a son lecteur - et le seul ou
    /// <c>Speed.Y</c> arrive intacte jusqu'au <c>MoveV</c> qui la consomme, la branche
    /// de gravite ayant ete sautee entre-temps.
    /// </summary>
    public static void NormalUpdate_prefix(Player __instance)
    {
      try
      {
        if (!SoccerPitch.Flat || __instance.Dead)
        {
          return;
        }

        setGround(__instance, true);

        var axis = (Vector2)moveAxis.GetValue(__instance);
        int index = __instance.PlayerIndex;

        // **En visee, la course est bridee par le jeu, et c'est ce qu'on defait.**
        //
        // Un archer qui vise plante ses pieds : NormalUpdate ramene sa vitesse
        // horizontale a zero des lors qu'il est au sol - et nous, on le declare au sol
        // en permanence. Charger une frappe revenait donc a s'immobiliser. On repose
        // donc la vitesse ici ; le freinage du jeu ne fait plus que la reduire d'un
        // cran, ce qui donne un deplacement legerement ralenti pendant la charge - un
        // effet qu'on aurait ecrit exprès.
        if (__instance.Aiming)
        {
          __instance.Speed.X = axis.X * SPEED;
        }

        Hop(__instance, index, axis);

        // **On EFFACE l'intention verticale une fois lue**, et c'est ce qui repare le
        // "bas tout seul ne fait rien".
        //
        // <c>InputDucking</c> vaut exactement "au sol, sans direction horizontale, vers
        // le bas" - c'est-a-dire la moitie de ce qu'on veut pouvoir faire, et le jeu y
        // repond par un <c>return</c> pose BIEN AVANT le deplacement. Corriger l'etat
        // rendu ne suffisait donc pas : l'image entiere etait deja perdue, aucun MoveV
        // n'ayant eu lieu. En diagonale la condition tombait a faux et l'archer
        // descendait - d'ou un bas qui ne marchait qu'accompagne.
        //
        // La verticale ayant deja servi, la mettre a zero ne coute rien : la visee lit
        // <c>AimAxis</c> et non cet axe, le sens du regard ne depend que de X, et les
        // autres lecteurs de cet axe vivent tous dans la branche "en l'air" ou l'on
        // n'est jamais.
        if (axis.Y != 0f)
        {
          moveAxis.SetValue(__instance, new Vector2(axis.X, 0f));
        }
      }
      catch (Exception e)
      {
        Logger.Error("FlatPlay.NormalUpdate_prefix: " + e);
      }
    }

    /// <summary>
    /// Empeche l'accroupissement, et lui seul.
    ///
    /// <c>NormalUpdate</c> rend le numero de l'etat suivant. On ne touche qu'a la
    /// reponse "accroupi" et on la remplace par "normal" : tout le reste du calcul -
    /// l'esquive, la mort, la prise de rebord - continue de decider comme d'habitude.
    /// Bloquer plus largement aurait fige l'archer dans son etat courant.
    /// </summary>
    /// <summary>
    /// Le saut sur place : un arc qui revient exactement d'ou il est parti.
    ///
    /// <b>Il est ecrit a la main parce que la gravite n'existe plus.</b> Le saut du jeu
    /// pose une impulsion et laisse la chute faire le reste ; sans chute, l'archer
    /// serait parti vers le haut pour ne jamais revenir. On retient donc la hauteur du
    /// depart, on applique NOTRE pesanteur le temps du vol, et l'on repose l'archer sur
    /// sa ligne exactement.
    ///
    /// <b>Sur un plateau vu de dessus, sauter n'est pas decoratif</b> : c'est ce qui
    /// permet d'enjamber un ballon qui roule sans le prendre - le ballon lit
    /// <see cref="Hopping"/> avant de se laisser ramasser. Un tacle, en somme, mais dans
    /// l'autre sens.
    /// </summary>
    private static void Hop(Player player, int index, Vector2 axis)
    {
      if (index < 0 || index >= hopping.Length)
      {
        player.Speed.Y = axis.Y * SPEED;
        return;
      }

      if (hopping[index])
      {
        hopSpeed[index] += HOP_GRAVITY;
        player.Speed.Y = hopSpeed[index];

        // On ne redescend pas plus bas que la ligne de depart : c'est elle, le sol.
        if (hopSpeed[index] > 0f && player.Y >= hopFloor[index])
        {
          hopping[index] = false;
          hopSpeed[index] = 0f;
          player.Speed.Y = 0f;

          // MoveV et non une ecriture directe : le deplacement du jeu s'arrete sur les
          // murs, une ecriture enfermerait l'archer dedans.
          player.MoveV(hopFloor[index] - player.Y, null);
        }

        return;
      }

      player.Speed.Y = axis.Y * SPEED;

      if (inputState == null)
      {
        return;
      }

      var state = (InputState)inputState.GetValue(player);

      if (state.JumpPressed)
      {
        hopping[index] = true;
        hopFloor[index] = player.Y;
        hopSpeed[index] = -HOP_SPEED;
        player.Speed.Y = -HOP_SPEED;

        Sounds.env_gplatPlayerJump.Play(player.X, 1f);
      }
    }

    /// <summary>
    /// Enleve l'arrondi a huit directions pendant une manche de foot.
    ///
    /// C'est le seul parametre a changer : le jeu calcule <c>snap</c> depuis la variante
    /// FreeAiming du joueur, et l'on repond a sa place sans rien ecrire dans ses
    /// reglages. La frappe se place alors a l'angle exact du stick, ce qui compte
    /// beaucoup plus au foot qu'a l'arc - une passe ratee de quinze degres ne trouve
    /// personne.
    /// </summary>
    public static void GetAimDirection_prefix(ref bool snap)
    {
      var settings = TFModFortRiseGameModeSoccerModule.Settings;

      if (settings != null && settings.preciseAim && SoccerMatch.Active)
      {
        snap = false;
      }
    }

    public static void NormalUpdate_postfix(ref int __result)
    {
      if (SoccerPitch.Flat && __result == DUCKING)
      {
        __result = NORMAL;
      }
    }
  }
}
