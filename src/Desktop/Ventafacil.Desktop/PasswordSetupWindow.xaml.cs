using System.Windows;

namespace Ventafacil.Desktop;

public partial class PasswordSetupWindow : Window
{
    public string Password => PasswordBox.Password;

    public PasswordSetupWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Password.Length < 6)
        {
            ValidationText.Text = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        if (!string.Equals(PasswordBox.Password, ConfirmPasswordBox.Password, StringComparison.Ordinal))
        {
            ValidationText.Text = "Las contraseñas no coinciden.";
            ConfirmPasswordBox.Clear();
            ConfirmPasswordBox.Focus();
            return;
        }

        DialogResult = true;
    }
}
