using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// L'indicateur de direction : ou partira la frappe.
  ///
  /// <b>Il remplace le viseur du jeu, qu'on avait du faire disparaitre.</b> Celui-ci est
  /// une pointe de FLECHE : il annoncait un tir qui n'arrive jamais dans ce mode, et il
  /// s'affichait a chaque coup de pied. Mais en le retirant on a retire aussi la seule
  /// chose qui disait dans quel sens on allait frapper - et sur un terrain plat, ou la
  /// visee couvre trois cent soixante degres, c'est justement l'information la plus
  /// utile de l'ecran.
  ///
  /// <b>Une seule entite pour tous les archers</b>, plutot qu'un indicateur attache a
  /// chacun. Elle se pose une fois par manche et parcourt les joueurs : rien a nettoyer
  /// quand un archer meurt, rien a rebrancher quand il revient.
  ///
  /// <b>Il n'apparait que lorsqu'il sert</b> : en train de charger, ou porteur du
  /// ballon. Affiche en permanence, il ferait huit fleches a l'ecran pour annoncer huit
  /// gestes dont sept n'auront pas lieu.
  /// </summary>
  public class SoccerAimer : Entity
  {
    /// <summary>Juste devant les archers, comme le ballon.</summary>
    private const int DEPTH = -104;

    /// <summary>Distance du repere au centre de l'archer, en pixels.</summary>
    private const float RADIUS = 13f;

    /// <summary>Longueur du trait qui relie l'archer a son repere.</summary>
    private const float TAIL = 6f;

    public SoccerAimer() : base(Vector2.Zero)
    {
      Depth = DEPTH;
    }

    public override void Render()
    {
      base.Render();

      var level = Scene as Level;

      if (level == null || !SoccerMatch.Active)
      {
        return;
      }

      try
      {
        foreach (Entity entity in level[GameTags.Player])
        {
          var player = entity as Player;

          if (player == null || player.Dead)
          {
            continue;
          }

          bool carrying = SoccerMatch.Ball != null && SoccerMatch.Ball.Carrier == player;

          if (!player.Aiming && !carrying)
          {
            continue;
          }

          Draw(player, carrying);
        }
      }
      catch (Exception e)
      {
        Logger.Error("SoccerAimer.Render: " + e.Message);
      }
    }

    /// <summary>
    /// Un trait et une pointe, a l'angle de visee de l'archer.
    ///
    /// <c>AimDirection</c> est ce que le jeu utilisera vraiment pour la frappe - on ne
    /// recalcule donc rien, on montre la valeur qui decide. C'est la difference entre un
    /// indicateur juste et un indicateur qui a l'air juste.
    ///
    /// La pointe grossit avec la CHARGE : l'archer voit sa puissance monter sans avoir a
    /// quitter des yeux la direction qu'il vise.
    /// </summary>
    private static void Draw(Player player, bool carrying)
    {
      float angle = player.AimDirection;
      Vector2 from = player.Position + new Vector2(0f, -5f);

      Vector2 direction = Calc.AngleToVector(angle, 1f);
      Vector2 tip = from + direction * RADIUS;

      Color color = ArcherData.GetColorA(player.PlayerIndex);

      // Le porteur qui ne charge pas encore n'a qu'un repere pale : il n'y a pas de
      // frappe engagee, seulement un sens de course.
      float strength = player.Aiming ? SoccerMatch.ChargeOf(player.PlayerIndex) : 0f;
      float alpha = player.Aiming ? 0.55f + 0.45f * strength : 0.35f;

      Monocle.Draw.Line(from + direction * TAIL, tip, Color.Black * alpha, 3f);
      Monocle.Draw.Line(from + direction * TAIL, tip, color * alpha, 1f);

      float size = 2f + 2f * strength;

      Monocle.Draw.Rect(tip.X - size - 1f, tip.Y - size - 1f,
          size * 2f + 2f, size * 2f + 2f, Color.Black * alpha);

      Monocle.Draw.Rect(tip.X - size, tip.Y - size, size * 2f, size * 2f, color * alpha);
    }
  }
}
