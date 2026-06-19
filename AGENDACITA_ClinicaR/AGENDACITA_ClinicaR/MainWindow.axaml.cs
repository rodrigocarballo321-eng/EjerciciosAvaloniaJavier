using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AGENDACITA_ClinicaR
{
    public partial class MainWindow : Window
    {
        private static readonly string[] Horarios =
            { "8:00", "8:30", "9:00", "9:30", "10:00", "10:30", "11:00", "11:30", "14:00" };

        private static readonly CultureInfo Es = new("es-ES");

        private readonly string[] _pacientes =
        {
            "Elena Hernández Cruz · DUI 04532109-7",
            "Roberto Antonio Ayala García · DUI 01234567-8",
            "Rosa María Cardona Pineda · DUI 04825731-2",
            "José Luis Martínez · Exp. 1023",
            "Ana Sofía Ramírez · Exp. 1187"
        };

        private ToggleButton? _slotSeleccionado;
        private bool _actualizando;
        public MainWindow()
        {
            InitializeComponent();

            PacienteBox.ItemsSource = _pacientes;

            Cal.DisplayDateStart = DateTime.Today;

            var inicial = SiguienteDiaHabil(DateTime.Today);
            Cal.SelectedDate = inicial;
            ConstruirHorarios(inicial);
            ActualizarResumen();
        }

        private static DateTime SiguienteDiaHabil(DateTime d)
        {
            while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                d = d.AddDays(1);
            return d;
        }

        private void OnFechaChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_actualizando) return;

            if (Cal.SelectedDate is { } fecha)
            {
                if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                {
                    _actualizando = true;
                    var habil = SiguienteDiaHabil(fecha);
                    Cal.SelectedDate = habil;
                    _actualizando = false;
                    ShowStatus("No se atienden citas en fin de semana. Se movió al siguiente día hábil.", false);
                    ConstruirHorarios(habil);
                }
                else
                {
                    ConstruirHorarios(fecha);
                }
            }
            ActualizarResumen();
        }

        private void ConstruirHorarios(DateTime fecha)
        {
            HorariosPanel.Children.Clear();
            _slotSeleccionado = null;

            for (int i = 0; i < Horarios.Length; i++)
            {
                var label = Horarios[i];
                bool ocupado = (fecha.Day + i) % 4 == 0;

                var tb = new ToggleButton { Classes = { "slot" } };

                if (ocupado)
                {
                    tb.Content = new TextBlock
                    {
                        Text = label,
                        TextDecorations = TextDecorations.Strikethrough,
                        Foreground = new SolidColorBrush(Color.Parse("#999999"))
                    };
                    tb.Background = new SolidColorBrush(Color.Parse("#F3F3F3"));
                    tb.IsEnabled = false;
                }
                else
                {
                    tb.Content = label;
                    tb.Tag = label;
                    tb.IsCheckedChanged += OnSlotChecked;
                }

                HorariosPanel.Children.Add(tb);
            }
        }

        private void OnSlotChecked(object? sender, EventArgs e)
        {
            if (_actualizando) return;
            if (sender is not ToggleButton tb) return;

            if (tb.IsChecked == true)
            {
                _actualizando = true;
                foreach (var child in HorariosPanel.Children)
                    if (child is ToggleButton otro && !ReferenceEquals(otro, tb))
                        otro.IsChecked = false;
                _actualizando = false;
                _slotSeleccionado = tb;
            }
            else if (ReferenceEquals(tb, _slotSeleccionado))
            {
                _slotSeleccionado = null;
            }
            ActualizarResumen();
        }

        private void OnResumenChanged(object? sender, SelectionChangedEventArgs e) => ActualizarResumen();

        private void ActualizarResumen()
        {
            string hora = _slotSeleccionado?.Tag as string ?? "";
            if (Cal.SelectedDate is { } fecha && hora != "")
            {
                var texto = fecha.ToString("dddd dd 'de' MMMM", Es);
                texto = char.ToUpper(texto[0]) + texto[1..];
                ResumenFecha.Text = $"{texto} · {FormatoHora(hora)}";
            }
            else if (Cal.SelectedDate is { } f2)
            {
                var texto = f2.ToString("dddd dd 'de' MMMM", Es);
                texto = char.ToUpper(texto[0]) + texto[1..];
                ResumenFecha.Text = $"{texto} · (elige una hora)";
            }
            else
            {
                ResumenFecha.Text = "Selecciona fecha y hora";
            }

            string medico = (MedicoCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Médico";
            ResumenMedico.Text = $"{medico} · Consultorio 2 · Duración estimada 30 min";
        }

        private static string FormatoHora(string h) => h == "14:00" ? "2:00 pm" : h + " am";

        private void OnAgendar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(PacienteBox.Text)) errores.Add("Selecciona el paciente.");
            if (MedicoCombo.SelectedItem is null) errores.Add("Selecciona el médico.");
            if (TipoCombo.SelectedItem is null) errores.Add("Selecciona el tipo de consulta.");
            if (Cal.SelectedDate is null) errores.Add("Selecciona una fecha.");
            if (_slotSeleccionado is null) errores.Add("Selecciona un horario disponible.");

            if (errores.Count > 0)
            {
                ShowStatus("Faltan datos:\n• " + string.Join("\n• ", errores), false);
                return;
            }

            string msg = "Cita agendada correctamente.";
            if (SmsSwitch.IsChecked == true)
                msg += " Recordatorio enviado al teléfono del paciente.";
            ShowStatus(msg, true);
        }

        private void OnCancelar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            PacienteBox.Text = "";
            MedicoCombo.SelectedItem = null;
            TipoCombo.SelectedItem = null;
            MotivoBox.Text = "";
            SmsSwitch.IsChecked = true;
            PrimeraSwitch.IsChecked = false;
            if (_slotSeleccionado != null) _slotSeleccionado.IsChecked = false;
            StatusBox.IsVisible = false;
            ActualizarResumen();
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