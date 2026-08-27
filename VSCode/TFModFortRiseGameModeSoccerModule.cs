using System;
using System.Diagnostics;
using FortRise;
using Microsoft.Extensions.Logging;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Le soccer : un ballon, deux buts, pas une seule fleche.
  ///
  /// Le ballon se prend en marchant dessus, se garde devant soi, se frappe au
  /// bouton de tir - d'autant plus fort qu'on l'a maintenu - et se perd quand on se
  /// fait bousculer ou pietiner. Il n'y a plus rien pour tuer : la manche se gagne
  /// en marquant.
  /// </summary>
  public class TFModFortRiseGameModeSoccerModule : Mod
  {
    public static TFModFortRiseGameModeSoccerModule Instance;

    // TextureRegistry en premier : le mode versus reference son illustration.
    private static Type[] Registerables = [
        typeof(TextureRegistry),
        typeof(SoccerGameMode),
    ];

    internal Type[] Hookables = [
        typeof(MyPlayer),
        typeof(MyPlayerOnPlayer),
        typeof(MyPlayerDeath),
        typeof(MyArrowHUD),
        typeof(MyVersusModeButton),
        typeof(FlatPlay),
    ];

    public static TFModFortRiseGameModeSoccerSettings Settings =>
        Instance != null
            ? Instance.GetSettings<TFModFortRiseGameModeSoccerSettings>()!
            : new TFModFortRiseGameModeSoccerSettings();

    public TFModFortRiseGameModeSoccerModule(IModContent content, IModuleContext context, ILogger logger)
        : base(content, context, logger)
    {
      if (!Debugger.IsAttached)
      {
        //Debugger.Launch(); // Proposera d'attacher Visual Studio
      }

      Instance = this;
      TFModFortRiseGameModeSoccer.Logger.Init(logger);

      foreach (var hookable in Hookables)
      {
        // Un accrochage qui echoue ne doit pas empecher le JEU de demarrer. Le prix
        // d'un reglage absent est une ligne de journal ; celui d'un jeu qui ne se
        // lance plus est la soiree.
        try
        {
          hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
        }
        catch (Exception e)
        {
          TFModFortRiseGameModeSoccer.Logger.Error(
              $"{hookable.Name} non accroche : {e.InnerException?.Message ?? e.Message}");
        }
      }

      foreach (var registerable in Registerables)
      {
        try
        {
          registerable.GetMethod(nameof(IRegisterable.Register))!
              .Invoke(null, [content, context.Registry]);
        }
        catch (Exception e)
        {
          TFModFortRiseGameModeSoccer.Logger.Error(
              $"{registerable.Name} non inscrit : {e.InnerException?.Message ?? e.Message}");
        }
      }
    }

    public override ModuleSettings CreateSettings()
    {
      return new TFModFortRiseGameModeSoccerSettings();
    }

    /// <summary>
    /// Ecrit les reglages sur disque tout de suite.
    ///
    /// FortRise ne les enregistre qu'en sortant du menu Options : une valeur changee
    /// dans la popup d'avant-match resterait en memoire et serait perdue au prochain
    /// lancement. SaveSettings est interne cote FortRise, d'ou la reflexion - le meme
    /// detour que les autres mods du depot.
    /// </summary>
    public static void SaveSettingsNow()
    {
      if (Instance == null)
      {
        return;
      }

      try
      {
        var method = typeof(Mod).GetMethod("SaveSettings",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method?.Invoke(Instance, null);
      }
      catch (Exception e)
      {
        TFModFortRiseGameModeSoccer.Logger.Info("[Soccer] reglages non enregistres : " + e.Message);
      }
    }

    /// <summary>
    /// Ce que les autres mods peuvent savoir du match : ou est le ballon, qui le
    /// porte, et vers quel but aller. L'IA en a besoin pour jouer au foot plutot que
    /// de poursuivre le joueur le plus proche.
    /// </summary>
    public override object GetApi()
    {
      return new ApiImplementation();
    }

  }
}
