using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;

namespace Beneficiario_ONG
{
    public partial class MainWindow : Window
    {
        private readonly HashSet<string> _documentosUsados = new(StringComparer.OrdinalIgnoreCase)
        {
            "01234567-8", "09876543-2", "04825731-2"
        };
        private readonly Dictionary<string, string[]> _municipios = new()
        {
            ["La Libertad"] = new[] { "Santa Tecla", "Colón", "Zaragoza", "Quezaltepeque", "San Juan Opico" },
            ["San Salvador"] = new[] { "San Salvador", "Soyapango", "Mejicanos", "Apopa", "Ilopango" },
            ["Santa Ana"] = new[] { "Santa Ana", "Chalchuapa", "Metapán", "Coatepeque" },
            ["San Miguel"] = new[] { "San Miguel", "Chinameca", "Ciudad Barrios", "Moncagua" },
            ["Sonsonate"] = new[] { "Sonsonate", "Izalco", "Acajutla", "Nahuizalco" }
        };

        private readonly Random _rng = new();

        public MainWindow()
        {
            InitializeComponent();

            foreach (var dep in _municipios.Keys)
                DepartamentoCombo.Items.Add(dep);
        }

        private void OnNombreChanged(object? sender, TextChangedEventArgs e)
        {
            var nombre = NombreBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(nombre))
            {
                AvatarText.Text = "?";
                return;
            }
            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var iniciales = partes.Length >= 2
                ? $"{partes[0][0]}{partes[1][0]}"
                : partes[0][0].ToString();
            AvatarText.Text = iniciales.ToUpper();
        }

        private void OnFechaChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            if (FechaNacimiento.SelectedDate is { } fecha)
            {
                var hoy = DateTime.Today;
                int edad = hoy.Year - fecha.Year;
                if (fecha.Date > hoy.AddYears(-edad)) edad--;
                EdadBox.Text = edad >= 0 ? edad.ToString() : "—";
            }
            else
            {
                EdadBox.Text = "";
            }
        }

        private void OnDepartamentoChanged(object? sender, SelectionChangedEventArgs e)
        {
            MunicipioCombo.Items.Clear();
            if (DepartamentoCombo.SelectedItem is string dep && _municipios.TryGetValue(dep, out var muns))
            {
                foreach (var m in muns)
                    MunicipioCombo.Items.Add(m);
            }
        }

        private void OnCambiarFoto(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowStatus("Para esta práctica la foto es opcional; se usan las iniciales como avatar.", false);
        }

        private void OnGuardarBorrador(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowStatus("Borrador guardado. Puedes completar el registro más tarde.", true);
        }

        private void OnRegistrar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var errores = new List<string>();

            var nombre = NombreBox.Text?.Trim() ?? "";
            if (nombre.Length < 8) errores.Add("El nombre completo debe tener al menos 8 caracteres.");

            var doc = DocumentoBox.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(doc)) errores.Add("El documento es obligatorio.");
            else if (_documentosUsados.Contains(doc)) errores.Add("El documento ya está registrado.");

            if (FechaNacimiento.SelectedDate is null) errores.Add("La fecha de nacimiento es obligatoria.");
            if (GeneroCombo.SelectedItem is null) errores.Add("Selecciona el género.");
            if (DepartamentoCombo.SelectedItem is null) errores.Add("Selecciona el departamento.");
            if (MunicipioCombo.SelectedItem is null) errores.Add("Selecciona el municipio.");
            if (string.IsNullOrWhiteSpace(DireccionBox.Text)) errores.Add("La dirección detallada es obligatoria.");
            if (!AutorizaSwitch.IsChecked.GetValueOrDefault())
                errores.Add("Debe autorizar el uso de sus datos para poder registrar.");

            if (errores.Count > 0)
            {
                ShowStatus("No se pudo registrar:\n• " + string.Join("\n• ", errores), false);
                return;
            }

            _documentosUsados.Add(doc);
            int id = _rng.Next(10000, 99999);
            ShowStatus($"Beneficiario registrado con ID #{id}.", true);
        }

        private void OnCancelar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NombreBox.Text = "";
            DocumentoBox.Text = "";
            OcupacionBox.Text = "";
            DireccionBox.Text = "";
            EdadBox.Text = "";
            FechaNacimiento.SelectedDate = null;
            GeneroCombo.SelectedItem = null;
            DepartamentoCombo.SelectedItem = null;
            MunicipioCombo.Items.Clear();
            foreach (var cb in new[] { Vul1, Vul2, Vul3, Vul4, Vul5, Vul6 }) cb.IsChecked = false;
            AutorizaSwitch.IsChecked = false;
            AvatarText.Text = "?";
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