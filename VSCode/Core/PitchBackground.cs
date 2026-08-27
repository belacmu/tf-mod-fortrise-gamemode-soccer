using System;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le terrain peint : la pelouse et ses lignes blanches, vus de dessus.
  ///
  /// <b>Une entite tres au FOND</b> plutot qu'un vrai fond de niveau. Le fond d'une
  /// tour appartient a la tour - il porte ses couleurs, ses nuages, ses animations - et
  /// le remplacer voudrait dire en fabriquer un, avec tout ce que le jeu attend d'un
  /// <c>Background</c>. Une entite posee derriere tout le reste couvre exactement ce
  /// qu'il faut : on voit le stade, et rien de la tour.
  ///
  /// <b>Le dessin s'arrete AVANT les murs</b>, aux bornes que <see cref="SoccerPitch"/>
  /// donne. Peindre par-dessus l'enceinte effacerait les tuiles que le jeu vient de
  /// choisir, et le stade n'aurait plus de bords.
  ///
  /// Les proportions sont celles d'un terrain reel, en fractions de la surface : elles
  /// suivent donc un stade elargi par WiderSet sans que ce code ait a le savoir.
  /// </summary>
  public class PitchBackground : Entity
  {
    /// <summary>Derriere tout : les profondeurs elevees sont dessinees en premier.</summary>
    private const int DEPTH = 20000;

    private static readonly Color Grass = Calc.HexToColor("2E6B37");
    private static readonly Color Stripe = Calc.HexToColor("35793F");
    private static readonly Color Paint = Calc.HexToColor("E8F2E8");

    /// <summary>Largeur d'une bande de tonte, en pixels.</summary>
    private const int MOW = 20;

    private readonly Rectangle area;

    public PitchBackground(Rectangle area) : base(0)
    {
      this.area = area;
      Depth = DEPTH;
      Position = Vector2.Zero;
    }

    public override void Render()
    {
      base.Render();

      Grid(area);
      Lines(area);
    }

    /// <summary>La pelouse et ses bandes de tonte, qui donnent l'echelle.</summary>
    private static void Grid(Rectangle a)
    {
      Draw.Rect(a.X, a.Y, a.Width, a.Height, Grass);

      for (int x = a.Left; x < a.Right; x += MOW * 2)
      {
        int width = Math.Min(MOW, a.Right - x);
        Draw.Rect(x, a.Y, width, a.Height, Stripe);
      }
    }

    /// <summary>
    /// Le trace reglementaire : touches, ligne mediane, rond central, surfaces de
    /// reparation et surfaces de but.
    /// </summary>
    private static void Lines(Rectangle a)
    {
      // Les touches, rentrees de quatre pixels : une ligne collee au mur se confond
      // avec lui.
      var pitch = new Rectangle(a.X + 4, a.Y + 4, a.Width - 8, a.Height - 8);
      Draw.HollowRect(pitch, Paint);

      int midX = pitch.X + pitch.Width / 2;

      // La mediane.
      Draw.Rect(midX, pitch.Y, 1f, pitch.Height, Paint);

      // Le rond central et son point.
      Circle(new Vector2(midX + 0.5f, pitch.Y + pitch.Height / 2f), pitch.Height * 0.18f);
      Draw.Rect(midX - 1f, pitch.Y + pitch.Height / 2f - 1f, 2f, 2f, Paint);

      // Les surfaces. Un terrain vu de dessus les porte a gauche et a droite, la ou
      // les buts se trouvent - c'est ce qui rend la lecture immediate.
      int boxDepth = (int)(pitch.Width * 0.14f);
      int boxHeight = (int)(pitch.Height * 0.55f);
      int boxTop = pitch.Y + (pitch.Height - boxHeight) / 2;

      Draw.HollowRect(pitch.Left, boxTop, boxDepth, boxHeight, Paint);
      Draw.HollowRect(pitch.Right - boxDepth, boxTop, boxDepth, boxHeight, Paint);

      int goalDepth = (int)(pitch.Width * 0.06f);
      int goalHeight = (int)(pitch.Height * 0.28f);
      int goalTop = pitch.Y + (pitch.Height - goalHeight) / 2;

      Draw.HollowRect(pitch.Left, goalTop, goalDepth, goalHeight, Paint);
      Draw.HollowRect(pitch.Right - goalDepth, goalTop, goalDepth, goalHeight, Paint);
    }

    /// <summary>
    /// Un cercle, en segments.
    ///
    /// Monocle ne dessine ni cercle ni arc : il n'a que le rectangle et le segment. On
    /// approche donc par un polygone, dont le nombre de cotes suit le rayon - assez
    /// pour que l'oeil n'y voie pas d'angle, pas assez pour couter quoi que ce soit.
    /// </summary>
    private static void Circle(Vector2 center, float radius)
    {
      int sides = Math.Max(16, (int)(radius * 1.2f));

      Vector2 previous = center + new Vector2(radius, 0f);

      for (int i = 1; i <= sides; i++)
      {
        float angle = MathHelper.TwoPi * i / sides;
        var next = center + new Vector2((float)Math.Cos(angle) * radius,
                                        (float)Math.Sin(angle) * radius);

        Draw.Line(previous, next, Paint);
        previous = next;
      }
    }
  }
}
