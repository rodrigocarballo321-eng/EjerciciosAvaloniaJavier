using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatriculasEscolaresR
{
    public partial class MainWindow : Window
    {
        private int _step;
        private readonly Random _rng = new();

        private static readonly SolidColorBrush Violet = new(Color.Parse("#6D28D9"));
        private static readonly SolidColorBrush Gray = new(Color.Parse("#E0E0E0"));
        private static readonly SolidColorBrush GrayText = new(Color.Parse("#6B6B6B"));
        private static readonly SolidColorBrush White = new(Colors.White);

        public MainWindow()
        {
            InitializeComponent();
            ActualizarUI();
        }

        private StackPanel[] Pasos => new[] { Step1, Step2, Step3, Step4 };

        private void ActualizarUI()
        {
            var pasos = Pasos;
            for (int i = 0; i < pasos.Length; i++)
                pasos[i].IsVisible = i == _step;

            PintarCirculo(Circle1, Num1, Lbl1, "1", 0);
            PintarCirculo(Circle2, Num2, Lbl2, "2", 1);
            PintarCirculo(Circle3, Num3, Lbl3, "3", 2);
            PintarCirculo(Circle4, Num4, Lbl4, "4", 3);
            Line1.Background = _step >= 1 ? Violet : Gray;
            Line2.Background = _step >= 2 ? Violet : Gray;
            Line3.Background = _step >= 3 ? Violet : Gray;

            BtnAnterior.IsVisible = _step > 0;
            BtnSiguiente.Content = _step == 3 ? "Confirmar matrícula" : "Siguiente ›";

            StatusBox.IsVisible = false;

            if (_step == 3) ConstruirResumen();
        }

        private void PintarCirculo(Border circle, TextBlock num, TextBlock lbl, string n, int idx)
        {
            if (idx < _step)
            {
                circle.Background = Violet;
                circle.BorderThickness = new Avalonia.Thickness(0);
                num.Text = "?";
                num.Foreground = White;
                lbl.Foreground = Violet;
            }
            else if (idx == _step)
            {
                circle.Background = Violet;
                circle.BorderThickness = new Avalonia.Thickness(0);
                num.Text = n;
                num.Foreground = White;
                lbl.Foreground = Violet;
            }
            else 
            {
                circle.Background = White;
                circle.BorderBrush = Gray;
                circle.BorderThickness = new Avalonia.Thickness(2);
                num.Text = n;
                num.Foreground = GrayText;
                lbl.Foreground = GrayText;
            }
        }

        private void OnParentescoChanged(object? sender, SelectionChangedEventArgs e)
        {
            var sel = (A_Parentesco.SelectedItem as ComboBoxItem)?.Content?.ToString();
            A_OtroPanel.IsVisible = sel == "Otro";
        }

        private void OnEmergenciaChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            A_PrioridadPanel.IsVisible = A_Emergencia.IsChecked == true;
        }

        private void OnSiguiente(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!ValidarPaso(_step)) return;

            if (_step < 3)
            {
                _step++;
                ActualizarUI();
            }
            else
            {
                int codigo = _rng.Next(100, 999);
                ShowStatus($"Matrícula registrada con código de inscripción #{codigo}-2026.", true);
            }
        }

        private void OnAnterior(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_step > 0)
            {
                _step--;
                ActualizarUI();
            }
        }

        private bool ValidarPaso(int paso)
        {
            var faltan = new List<string>();
            if (paso == 0)
            {
                if (string.IsNullOrWhiteSpace(E_Nombres.Text)) faltan.Add("Nombres");
                if (string.IsNullOrWhiteSpace(E_Apellidos.Text)) faltan.Add("Apellidos");
                if (E_Fecha.SelectedDate is null) faltan.Add("Fecha de nacimiento");
                if (string.IsNullOrWhiteSpace(E_Documento.Text)) faltan.Add("DUI / Carnet");
                if (E_Genero.SelectedItem is null) faltan.Add("Género");
                if (E_Grado.SelectedItem is null) faltan.Add("Grado");
                if (string.IsNullOrWhiteSpace(E_Direccion.Text)) faltan.Add("Dirección");
            }
            else if (paso == 1)
            {
                if (A_Parentesco.SelectedItem is null) faltan.Add("Parentesco");
                else if (Texto(A_Parentesco) == "Otro" && string.IsNullOrWhiteSpace(A_OtroParentesco.Text))
                    faltan.Add("Especificar parentesco");
                if (string.IsNullOrWhiteSpace(A_Dui.Text)) faltan.Add("DUI del acudiente");
                if (string.IsNullOrWhiteSpace(A_Nombres.Text)) faltan.Add("Nombres del acudiente");
                if (string.IsNullOrWhiteSpace(A_Apellidos.Text)) faltan.Add("Apellidos del acudiente");
                if (string.IsNullOrWhiteSpace(A_Telefono.Text)) faltan.Add("Teléfono");
            }

            if (faltan.Count > 0)
            {
                ShowStatus("Completa los campos obligatorios antes de continuar:\n• " + string.Join("\n• ", faltan), false);
                return false;
            }
            return true;
        }

        private void ConstruirResumen()
        {
            string parentesco = Texto(A_Parentesco);
            if (parentesco == "Otro") parentesco = A_OtroParentesco.Text ?? "Otro";

            var docs = new List<string>();
            foreach (var (cb, nom) in new[]
            {
                (D1, "Partida de nacimiento"), (D2, "Foto carnet"), (D3, "Constancia último año"),
                (D4, "Tarjeta de vacunas"), (D5, "DUI acudiente"), (D6, "Constancia solvencia"),
                (D7, "Comprobante domicilio"), (D8, "Boletín anterior")
            })
                if (cb.IsChecked == true) docs.Add(nom);

            var sb = new StringBuilder();
            sb.AppendLine("ESTUDIANTE");
            sb.AppendLine($"   {E_Nombres.Text} {E_Apellidos.Text}");
            sb.AppendLine($"   Documento: {E_Documento.Text}   ·   Género: {Texto(E_Genero)}");
            sb.AppendLine($"   Grado: {Texto(E_Grado)}   ·   Sección: {Texto(E_Seccion)}");
            sb.AppendLine($"   Dirección: {E_Direccion.Text}");
            sb.AppendLine();
            sb.AppendLine("ACUDIENTE");
            sb.AppendLine($"   {A_Nombres.Text} {A_Apellidos.Text}  ({parentesco})");
            sb.AppendLine($"   DUI: {A_Dui.Text}   ·   Tel: {A_Telefono.Text}");
            if (!string.IsNullOrWhiteSpace(A_Correo.Text)) sb.AppendLine($"   Correo: {A_Correo.Text}");
            sb.AppendLine();
            sb.AppendLine("DOCUMENTOS ENTREGADOS");
            sb.AppendLine(docs.Count > 0 ? "   " + string.Join(", ", docs) : "   (ninguno marcado)");

            ResumenText.Text = sb.ToString();
        }

        private static string Texto(ComboBox cb) =>
            (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "—";

        private void OnCancelar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowStatus("Matrícula cancelada. (En un sistema real se ofrecería guardar como borrador.)", false);
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
