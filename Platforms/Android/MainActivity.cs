using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Notas
{
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              LaunchMode = LaunchMode.SingleTop,
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Asegurar que la pantalla se ajuste automáticamente cuando se muestra el teclado
            Window.SetSoftInputMode(SoftInput.AdjustResize);
        }
    }
}
