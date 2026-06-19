using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace SolicitudD_CreditoR
{
    public partial class MainWindow : Window
    {
        private const double TasaMensual = 0.12 / 12.0; 
        private static readonly CultureInfo Usd = CultureInfo.GetCultureInfo("en-US");

        private readonly Dictionary<string, string> _socios = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CAS-00342"] = "Roberto Antonio Ayala García",
            ["CAS-00118"] = "María Estela Portillo Cruz",
            ["CAS-00277"] = "José Mauricio Hernández Lima",
            ["CAS-00405"] = "Ana Gloria Menjívar Soto"
        };

        private bool _sync;
        private readonly Random _rng = new();

        public MainWindow()
        {
            InitializeComponent();

            MontoSlider.PropertyChanged += (s, e) =>
            {
                if (e.Property == RangeBase.ValueProperty && !_sync)
                {
                    _sync = true;
                    ActualizarMontoBox(MontoSlider.Value);
                    _sync = false;
                    Calcular();
                }
            };

            PlazoCombo.SelectedIndex = 1; 
            MontoSlider.Value = 1000;
            ActualizarMontoBox(1000);
            Calcular();
        }

        private void OnMontoBoxLostFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)
        {
            if (_sync) return;
            var limpio = new string((MontoBox.Text ?? "").Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (double.TryParse(limpio, NumberStyles.Any, Usd, out var valor))
            {
                valor = Math.Clamp(valor, 200, 5000);
                valor = Math.Round(valor / 50.0) * 50.0;
                _sync = true;
                MontoSlider.Value = valor;
                ActualizarMontoBox(valor);
                _sync = false;
                Calcular();
            }
            else
            {
                ActualizarMontoBox(MontoSlider.Value);
            }
        }

        private void ActualizarMontoBox(double valor) =>
            MontoBox.Text = "$ " + valor.ToString("N2", Usd);

        private void OnPlazoChanged(object? sender, SelectionChangedEventArgs e) => Calcular();
        private void Calcular()
        {
            double monto = MontoSlider.Value;
            int n = PlazoMeses();
            if (n <= 0) return;

            double i = TasaMensual;
            double factor = Math.Pow(1 + i, n);
            double cuota = monto * (i * factor) / (factor - 1);
            double total = cuota * n;
            double intereses = total - monto;

            CuotaText.Text = "$ " + cuota.ToString("N2", Usd);
            TotalText.Text = "Total a pagar: $ " + total.ToString("N2", Usd);
            InteresText.Text = "Intereses: $ " + intereses.ToString("N2", Usd);
        }

        private int PlazoMeses()
        {
            var txt = (PlazoCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var num = new string(txt.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(num, out var m) ? m : 0;
        }

        private void OnCodigoLostFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)
        {
            var cod = CodigoBox.Text?.Trim() ?? "";
            NombreBox.Text = _socios.TryGetValue(cod, out var nombre) ? nombre : "";
        }

        private void OnOtroDestinoChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OtroDestinoPanel.IsVisible = Dest6.IsChecked == true;
        }

        private void OnEnviar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var errores = new List<string>();

            var cod = CodigoBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(cod) || !_socios.ContainsKey(cod))
                errores.Add("Código de socio inválido o no registrado.");

            bool algunDestino = new[] { Dest1, Dest2, Dest3, Dest4, Dest5, Dest6 }
                .Any(c => c.IsChecked == true);
            if (!algunDestino) errores.Add("Selecciona al menos un destino del crédito.");
            if (Dest6.IsChecked == true && string.IsNullOrWhiteSpace(OtroDestinoBox.Text))
                errores.Add("Especifica el destino 'Otro'.");

            if ((JustificacionBox.Text ?? "").Trim().Length < 30)
                errores.Add("La justificación debe tener al menos 30 caracteres.");

            if (ReglamentoSwitch.IsChecked != true)
                errores.Add("Debes aceptar el reglamento de crédito.");

            if (errores.Count > 0)
            {
                ShowStatus("No se pudo enviar:\n• " + string.Join("\n• ", errores), false);
                return;
            }

            int id = _rng.Next(100, 999);
            ShowStatus($"Solicitud #SC-{id} enviada para revisión.", true);
        }

        private void OnGuardarBorrador(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowStatus("Borrador guardado. Puedes completar la solicitud más tarde.", true);
        }

        private void OnCancelar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CodigoBox.Text = "";
            NombreBox.Text = "";
            MontoSlider.Value = 1000;
            PlazoCombo.SelectedIndex = 1;
            foreach (var c in new[] { Dest1, Dest2, Dest3, Dest4, Dest5, Dest6 }) c.IsChecked = false;
            OtroDestinoBox.Text = "";
            JustificacionBox.Text = "";
            ReglamentoSwitch.IsChecked = false;
            StatusBox.IsVisible = false;
            Calcular();
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
