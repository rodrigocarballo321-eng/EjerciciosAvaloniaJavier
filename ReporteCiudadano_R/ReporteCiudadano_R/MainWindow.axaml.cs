using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace ReporteCiudadano_R
{
    public partial class MainWindow : Window
    {
        private static readonly (string Emoji, string Nombre)[] Tipos =
        {
            ("??", "Bache en la calle"),
            ("??", "Alumbrado público"),
            ("??", "Basura acumulada"),
            ("??", "Fuga de agua"),
            ("??", "Árbol caído"),
            ("?", "Otro")
        };

        private static readonly string[] Niveles = { "Leve", "Bajo", "Moderado", "Alto", "Urgente" };

        private ToggleButton? _tipoSeleccionado;
        private readonly List<string> _fotos = new();
        private bool _actualizando;
        private readonly Random _rng = new();

        public MainWindow()
        {
            InitializeComponent();

            ConstruirTarjetas();

            SeveridadSlider.PropertyChanged += (s, e) =>
            {
                if (e.Property == RangeBase.ValueProperty) ActualizarSeveridad();
            };
            ActualizarSeveridad();
        }

        private void ConstruirTarjetas()
        {
            for (int i = 0; i < Tipos.Length; i++)
            {
                var (emoji, nombre) = Tipos[i];
                var card = new ToggleButton { Classes = { "card" }, Tag = nombre };
                card.Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = emoji, FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = nombre, FontSize = 13, FontWeight = FontWeight.SemiBold,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        TextWrapping = TextWrapping.Wrap, Margin = new Avalonia.Thickness(0,6,0,0) }
                    }
                };
                card.IsCheckedChanged += OnTipoChecked;
                CardsPanel.Children.Add(card);
            }

            if (CardsPanel.Children[0] is ToggleButton primera)
                primera.IsChecked = true;
        }

        private void OnTipoChecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_actualizando) return;
            if (sender is not ToggleButton tb) return;

            if (tb.IsChecked == true)
            {
                _actualizando = true;
                foreach (var child in CardsPanel.Children)
                    if (child is ToggleButton otro && !ReferenceEquals(otro, tb))
                        otro.IsChecked = false;
                _actualizando = false;
                _tipoSeleccionado = tb;
            }
            else if (ReferenceEquals(tb, _tipoSeleccionado))
            {
                _actualizando = true;
                tb.IsChecked = true;
                _actualizando = false;
            }

            OtroTipoPanel.IsVisible = (_tipoSeleccionado?.Tag as string) == "Otro";
        }

        private void ActualizarSeveridad()
        {
            int v = (int)Math.Round(SeveridadSlider.Value);
            v = Math.Clamp(v, 1, 5);

            var color = ColorSeveridad(v);
            var bars = new[] { Bar1, Bar2, Bar3, Bar4, Bar5 };
            for (int i = 0; i < bars.Length; i++)
                bars[i].Background = i < v ? new SolidColorBrush(color)
                                           : new SolidColorBrush(Color.Parse("#E0E0E0"));

            SeveridadLabel.Text = Niveles[v - 1];
            SeveridadLabel.Foreground = new SolidColorBrush(color);
        }

        private static Color ColorSeveridad(int v) => v switch
        {
            1 or 2 => Color.Parse("#22C55E"), 
            3 => Color.Parse("#F59E0B"),      
            4 => Color.Parse("#F97316"),      
            _ => Color.Parse("#EF4444")       
        };

        private async void OnGps(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GpsBtn.IsEnabled = false;
            var textoOriginal = GpsBtn.Content;
            GpsBtn.Content = "Usando GPS...";
            await Task.Delay(1500); 
            DireccionBox.Text = "Lat: 13.7041, Lng: -89.3567 · Cantón Lourdes";
            GpsBtn.Content = textoOriginal;
            GpsBtn.IsEnabled = true;
        }

        private async void OnSubirFotos(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var top = GetTopLevel(this);
            if (top is null) return;

            var archivos = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Selecciona fotos del incidente",
                AllowMultiple = true,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageJpg, FilePickerFileTypes.ImagePng }
            });

            foreach (var f in archivos)
            {
                if (_fotos.Count >= 3)
                {
                    ShowStatus("Solo se permiten hasta 3 fotos.", false);
                    break;
                }
                var props = await f.GetBasicPropertiesAsync();
                if (props.Size is > 5 * 1024 * 1024)
                {
                    ShowStatus($"La foto '{f.Name}' supera los 5 MB y no se adjuntó.", false);
                    continue;
                }
                _fotos.Add(f.Name);
            }

            FotosLabel.Text = _fotos.Count == 0
                ? "Haz clic para subir una foto"
                : $"{_fotos.Count} foto(s) adjunta(s): {string.Join(", ", _fotos)}";
        }

        private void OnEnviar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var errores = new List<string>();

            if (_tipoSeleccionado is null) errores.Add("Selecciona el tipo de incidente.");
            else if ((_tipoSeleccionado.Tag as string) == "Otro" && string.IsNullOrWhiteSpace(OtroTipoBox.Text))
                errores.Add("Describe el tipo de incidente ('Otro').");

            if (string.IsNullOrWhiteSpace(NombreBox.Text)) errores.Add("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(TelefonoBox.Text))
                errores.Add(SmsSwitch.IsChecked == true
                    ? "El teléfono es obligatorio para recibir actualizaciones por SMS."
                    : "El teléfono es obligatorio.");

            if (errores.Count > 0)
            {
                ShowStatus("No se pudo enviar:\n• " + string.Join("\n• ", errores), false);
                return;
            }

            int id = _rng.Next(10000, 99999);
            ShowStatus($"Reporte #RC-{id} enviado. Recibirás respuesta en 5 días hábiles.", true);
        }

        private void OnCancelar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DireccionBox.Text = "";
            OtroTipoBox.Text = "";
            NombreBox.Text = "";
            TelefonoBox.Text = "";
            SmsSwitch.IsChecked = true;
            SeveridadSlider.Value = 3;
            _fotos.Clear();
            FotosLabel.Text = "Haz clic para subir una foto";
            StatusBox.IsVisible = false;
        }

        private void ShowStatus(string mensaje, bool ok)
        {
            StatusText.Text = mensaje;
            StatusText.Foreground = ok ? new SolidColorBrush(Color.Parse("#047857"))
                                       : new SolidColorBrush(Color.Parse("#991B1B"));
            StatusBox.Background = ok ? new SolidColorBrush(Color.Parse("#D1FAE5"))
                                      : new SolidColorBrush(Color.Parse("#FEE2E2"));
            StatusBox.IsVisible = true;
        }
    }
}