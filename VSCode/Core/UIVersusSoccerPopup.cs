using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Les reglages de terrain, ouverts avec Y depuis le bouton de mode.
  ///
  /// Meme geste que dans les mods survie, chat et handicap : ces reglages existent
  /// dans les options, mais on veut en changer juste avant de lancer, sans quitter le
  /// menu versus. C'est particulierement vrai ici - on ne sait pas d'avance si l'on
  /// prefere le stade ou une vraie tour, ni si l'apesanteur plait, et la seule facon
  /// de le savoir est d'alterner d'une partie a l'autre.
  /// </summary>
  public class UIVersusSoccerPopup : Entity
  {
    private readonly BorderButton ownerButton;

    private int row;

    public static UIVersusSoccerPopup Current;

    public static bool IsOpen => Current != null && Current.Scene == Engine.Instance.Scene;

    public UIVersusSoccerPopup(BorderButton ownerButton)
    {
      this.ownerButton = ownerButton;
      Position = new Vector2(160f, 120f);
    }

    public override void Added()
    {
      base.Added();
      Current = this;

      if (ownerButton != null)
      {
        ownerButton.Selected = false;
      }

      // Bloque le menu derriere : sans cela Back reviendrait a l'ecran precedent et
      // Start lancerait le match pendant qu'on regle le terrain.
      if (Scene is MainMenu menu)
      {
        menu.CanAct = false;
      }

      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();

      TFModFortRiseGameModeSoccerModule.SaveSettingsNow();

      if (Current == this)
      {
        Current = null;
      }

      Sounds.ui_unpause.Play(160f);
      MenuInput.Clear();

      if (Scene is MainMenu menu)
      {
        menu.CanAct = true;
      }

      if (ownerButton != null)
      {
        ownerButton.Selected = true;
      }
    }

    /// <summary>Une ligne : son libelle, sa valeur lue, et de quoi la changer.</summary>
    private sealed class Row
    {
      public string Title;
      public string Hint;
      public Func<string> Read;
      public Action Toggle;
    }

    private static List<Row> Rows()
    {
      var s = TFModFortRiseGameModeSoccerModule.Settings;

      return new List<Row>
      {
        new Row
        {
          Title = "SOCCER FIELD",
          Hint = "EMPTY WALLED STADIUM INSTEAD OF THE TOWER",
          Read = () => s.field ? "ON" : "OFF",
          Toggle = () => s.field = !s.field,
        },
        new Row
        {
          Title = "TOP-DOWN PLAY",
          Hint = "NO GRAVITY: MOVE IN FOUR DIRECTIONS",
          Read = () => s.flat ? "ON" : "OFF",
          Toggle = () => s.flat = !s.flat,
        },
      };
    }

    public override void Update()
    {
      base.Update();
      MenuInput.Update();

      List<Row> rows = Rows();

      if (MenuInput.Down)
      {
        row = (row + 1) % rows.Count;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Up)
      {
        row = (row - 1 + rows.Count) % rows.Count;
        Sounds.ui_move1.Play(160f, 1f);
        return;
      }

      if (MenuInput.Left || MenuInput.Right)
      {
        rows[row].Toggle();
        Sounds.ui_click.Play(160f, 1f);
        return;
      }

      if (MenuInput.Confirm || MenuInput.Back)
      {
        RemoveSelf();
      }
    }

    public override void Render()
    {
      Draw.Rect(0, 0, 320, 240, Color.Black * 0.8f);

      List<Row> rows = Rows();

      Draw.OutlineTextCentered(TFGame.Font, "SOCCER FIELD",
          new Vector2(160f, 24f), Color.White, 2f);

      Draw.TextCentered(TFGame.Font, "UP/DOWN: PICK   LEFT/RIGHT: CHANGE",
          new Vector2(160f, 44f), Color.Gray);

      for (int i = 0; i < rows.Count; i++)
      {
        Row entry = rows[i];
        bool here = i == row;
        float y = 84f + i * 32f;

        Color color = here ? Calc.HexToColor("FFEC5E") : Color.White;

        Draw.TextCentered(TFGame.Font, entry.Title, new Vector2(110f, y), color);
        Draw.TextCentered(TFGame.Font, entry.Read(), new Vector2(240f, y), color);

        if (here)
        {
          Draw.TextCentered(TFGame.Font, ">", new Vector2(30f, y), color);

          // L'explication ne s'affiche que pour la ligne pointee : deux phrases
          // permanentes rempliraient l'ecran pour dire ce dont on ne s'occupe pas.
          Draw.TextCentered(TFGame.Font, entry.Hint, new Vector2(160f, y + 12f), Color.Gray);
        }
      }

      Draw.TextCentered(TFGame.Font, "A/B: CLOSE", new Vector2(160f, 210f), Color.Gray);
    }
  }
}
