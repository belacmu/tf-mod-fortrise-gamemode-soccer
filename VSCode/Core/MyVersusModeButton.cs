using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Ouvre les reglages de terrain avec Y, quand SOCCER est le mode choisi.
  ///
  /// Meme mecanique que dans les mods survie, chat et handicap - au point que le
  /// libelle est pose au meme endroit sous le bouton. Un geste appris une fois doit
  /// marcher partout : c'est la seule facon qu'il soit trouve.
  /// </summary>
  public class MyVersusModeButton : IHookable
  {
    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusModeButton), nameof(VersusModeButton.Update)),
          prefix: new HarmonyMethod(typeof(MyVersusModeButton), nameof(Update_patch)));

      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusModeButton), nameof(VersusModeButton.Render)),
          postfix: new HarmonyMethod(typeof(MyVersusModeButton), nameof(Render_patch)));

      // Tant que la popup est ouverte on ne lance pas le match : la scene du menu
      // serait remplacee sans que la popup soit fermee.
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(VersusMapButton), nameof(VersusMapButton.OnConfirm)),
          prefix: new HarmonyMethod(typeof(MyVersusModeButton), nameof(MapConfirm_patch)));
    }

    /// <summary>
    /// Le mode est enregistre par le mod : FortRise lui attribue une valeur de Modes,
    /// et l'on compare dessus plutot que sur un nom.
    /// </summary>
    private static bool IsSoccer(MatchSettings settings)
    {
      return settings != null
          && SoccerGameMode.Mode != null
          && settings.Mode == SoccerGameMode.Mode.Modes;
    }

    private static bool MapConfirm_patch()
    {
      return !UIVersusSoccerPopup.IsOpen;
    }

    private static bool AnyPlayerArrowsPressed()
    {
      for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
      {
        PlayerInput input = TFGame.PlayerInputs[i];

        if (input != null && input.GetState().ArrowsPressed)
        {
          return true;
        }
      }

      return false;
    }

    public static bool Update_patch(VersusModeButton __instance)
    {
      if (!IsSoccer(MainMenu.VersusMatchSettings))
      {
        return true;
      }

      if (__instance.Selected && !UIVersusSoccerPopup.IsOpen && AnyPlayerArrowsPressed())
      {
        if (__instance.Scene != null)
        {
          Sounds.ui_click.Play(160f, 1f);
          __instance.Scene.Add(new UIVersusSoccerPopup(__instance));
        }

        return false;
      }

      return true;
    }

    public static void Render_patch(VersusModeButton __instance)
    {
      if (!__instance.Selected || UIVersusSoccerPopup.IsOpen)
      {
        return;
      }

      if (!IsSoccer(MainMenu.VersusMatchSettings))
      {
        return;
      }

      Draw.OutlineTextCentered(TFGame.Font, "Y: FIELD",
          __instance.Position + new Vector2(0f, 22f), Calc.HexToColor("FFEC5E"), 1f);
    }
  }
}
