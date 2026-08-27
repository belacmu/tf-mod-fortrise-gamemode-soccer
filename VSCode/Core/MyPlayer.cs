using System;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le bouton de tir devient un coup de pied.
  ///
  /// Rien n'est reinvente cote entree : le jeu pose deja <c>Aiming</c> tant qu'on
  /// maintient le bouton de tir, et appelle <c>ShootArrow</c> au relachement. On se
  /// contente donc de MESURER la duree de la visee - c'est la puissance du tir - et
  /// d'intercepter le relachement.
  ///
  /// L'archer vise pendant qu'il charge, exactement comme avec un arc : la
  /// direction du coup de pied est celle de la visee, en huit directions ou libre
  /// selon la variante Free Aiming du jeu.
  /// </summary>
  public class MyPlayer : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Update)),
          postfix: new HarmonyMethod(Update_patch)
      );

      // ShootArrow est privee : nom en dur, pas de nameof possible. Prefix, parce
      // qu'il faut POUVOIR sauter la creation de la fleche.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(Player), "ShootArrow"),
          prefix: new HarmonyMethod(ShootArrow_patch)
      );
    }

    public static void Update_patch(Player __instance)
    {
      try
      {
        if (!SoccerMatch.Active || __instance.Dead)
        {
          return;
        }

        if (__instance.Aiming)
        {
          SoccerMatch.Charge(__instance);
        }

        HideBow(__instance);
      }
      catch (Exception e)
      {
        Logger.Error("MyPlayer.Update: " + e);
      }
    }

    /// <summary>
    /// Range l'arc et le viseur pour de bon.
    ///
    /// Le jeu remet leur visibilite a chaque image, depuis UpdateSprite et
    /// UpdateVisibility : il la calcule d'apres la pose et la visee, et les remonte
    /// donc des qu'on charge un tir. On repasse derriere lui, apres son Update et
    /// avant le rendu, plutot que d'aller patcher deux methodes privees qui prennent
    /// cette decision a huit endroits.
    ///
    /// Les sprites restent en place : les retirer priverait le corps de son ancrage
    /// d'arc, que d'autres poses lisent encore.
    ///
    /// Le viseur est la pointe de fleche qui suit la direction visee. Ici on ne vise
    /// pas une cible, on arme un coup de pied - et la fleche annonce un tir qui
    /// n'arrivera jamais.
    ///
    /// <b>Le viseur ne se cache PAS par sa visibilite, contrairement a l'arc</b>, et
    /// c'est ce qui le laissait apparaitre a chaque frappe. Les deux ne sont pas
    /// decides au meme moment : <c>bowSprite.Visible</c> est pose pendant l'update,
    /// donc apres lui on a le dernier mot ; <c>aimer.Visible</c>, lui, est reecrit
    /// dans <c>Player.DoWrapRender</c> - c'est-a-dire au moment du DESSIN, apres tout
    /// correctif pose sur l'update, et juste avant le trait qui le trace.
    ///
    /// On le reduit donc a une taille nulle, ce que rien ne remet en place : le jeu
    /// ecrit la rotation du viseur, sa visibilite et sa couleur, jamais son echelle.
    /// C'est la seule prise qui tienne des deux cotes de la barriere.
    /// </summary>
    private static void HideBow(Player player)
    {
      using var data = DynamicData.For(player);

      var bow = data.Get<Sprite<string>>("bowSprite");
      if (bow != null)
      {
        bow.Visible = false;
      }

      var aimer = data.Get<Image>("aimer");
      if (aimer != null)
      {
        aimer.Visible = false;
        aimer.Scale = Vector2.Zero;
      }
    }

    /// <summary>
    /// Rendre false saute le tir vanilla. En soccer on le saute TOUJOURS : il n'y a
    /// pas de fleche dans ce mode, et laisser passer l'appel ferait jouer l'echec
    /// du carquois vide a chaque coup de pied.
    /// </summary>
    public static bool ShootArrow_patch(Player __instance)
    {
      if (!SoccerMatch.Active)
      {
        return true;
      }

      try
      {
        // Sortir de la visee est la premiere ligne du ShootArrow vanilla, et elle
        // est obligatoire : sans elle l'archer reste en Aiming, et Player.Update
        // rappelle ShootArrow a CHAQUE image tant que le bouton est relache.
        __instance.Aiming = false;

        SoccerMatch.Kick(__instance);
      }
      catch (Exception e)
      {
        Logger.Error("MyPlayer.ShootArrow: " + e);
      }

      return false;
    }
  }
}
