using FortRise;

namespace TFModFortRiseGameModeSoccer
{
  /// <summary>
  /// Les deux reglages du terrain.
  ///
  /// <b>Ils sont separes alors qu'ils vont ensemble</b>, et c'est voulu. Vider la tour
  /// pour en faire un stade est une modification sure : on sait a quoi elle ressemble.
  /// Enlever la gravite, en revanche, change la nature meme du jeu - c'est
  /// l'experience, celle dont on ne sait pas encore si elle est bonne. Les melanger
  /// dans un seul interrupteur obligerait a renoncer au stade pour renoncer a
  /// l'apesanteur.
  ///
  /// Les deux sont EN MARCHE par defaut : le stade plat est ce que ce mode veut etre,
  /// et le decouvrir demande de le voir. Qui prefere le foot sur une vraie tour, avec
  /// ses plateformes et sa gravite, les eteint - c'est le sens de la separation.
  /// </summary>
  public class TFModFortRiseGameModeSoccerSettings : ModuleSettings
  {
    public override void Create(ISettingsCreate settings)
    {
      settings.CreateOnOff("Soccer field", field, (x) => { field = x; Save(); });
      settings.CreateOnOff("Top-down play (no gravity)", flat, (x) => { flat = x; Save(); });
      settings.CreateOnOff("Precise aim", preciseAim, (x) => { preciseAim = x; Save(); });
    }

    /// <summary>
    /// Remplace la tour par un stade : une salle vide, muree, avec un terrain peint
    /// au sol.
    /// </summary>
    public bool field { get; set; } = true;

    /// <summary>
    /// Le terrain devient PLAT : plus de chute, plus d'accroupissement, on se deplace
    /// dans les quatre directions comme sur un plateau vu de dessus.
    /// </summary>
    public bool flat { get; set; } = true;

    /// <summary>
    /// La frappe part a l'angle exact du stick, sans arrondi a huit directions.
    ///
    /// Cela compte beaucoup plus au foot qu'a l'arc : une passe ratee de quinze degres
    /// ne trouve personne, alors qu'une fleche a quinze degres pres touche encore. Ce
    /// n'est PAS la variante du jeu - on ne touche pas aux reglages du joueur, on
    /// repond seulement a sa place a la question que le jeu se pose au moment de viser.
    /// </summary>
    public bool preciseAim { get; set; } = true;

    private void Save()
    {
      TFModFortRiseGameModeSoccerModule.SaveSettingsNow();
    }
  }
}
