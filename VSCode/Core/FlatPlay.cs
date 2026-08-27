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

    public static void Load(IHarmony harmony)
    {
      MethodInfo setter = AccessTools.PropertySetter(typeof(Player), "OnGround");
      moveAxis = AccessTools.Field(typeof(Player), "moveAxis");

      if (setter != null)
      {
        setGround = (Action<Player, bool>)setter.CreateDelegate(typeof(Action<Player, bool>));
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

        __instance.Speed.Y = axis.Y * SPEED;
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
    public static void NormalUpdate_postfix(ref int __result)
    {
      if (SoccerPitch.Flat && __result == DUCKING)
      {
        __result = NORMAL;
      }
    }
  }
}
