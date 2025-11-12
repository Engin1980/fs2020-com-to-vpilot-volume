using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Eng.Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for SettingsEditor.xaml
  /// </summary>
  public partial class SettingsEditor : Window
  {
    public record ErrorItem(string Type, string Message, string Location);

    public static DependencyProperty ErrorsProperty = DependencyProperty.Register(
      nameof(Errors),
      typeof(BindingList<ErrorItem>),
      typeof(SettingsEditor),
      new PropertyMetadata(new BindingList<ErrorItem>())
    );

    public BindingList<ErrorItem> Errors
    {
      get { return (BindingList<ErrorItem>)GetValue(ErrorsProperty); }
      set { SetValue(ErrorsProperty, value); }
    }

    private readonly string jsonSchema;

    public SettingsEditor()
    {
      InitializeComponent();

      using var reader = new System.Xml.XmlTextReader("Resources/Json.xshd");
      var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
      txtJson.SyntaxHighlighting = highlighting;

      this.jsonSchema = System.IO.File.ReadAllText("appsettings.schema.json");
      LoadUserJsonFile();
    }

    private void LoadUserJsonFile()
    {
      var json = System.IO.File.ReadAllText(Eng.Com2vPilotVolume.Types.SettingsProvider.UserConfigFilePath);
      txtJson.Text = json;
    }

    private void ProcessTextChanged()
    {
      string json = txtJson.Text;
      var errs = ValidateJson(json, jsonSchema);
      Errors.Clear();
      foreach (var err in errs)
      {
        Errors.Add(err);
      }
    }

    /// <summary>
    /// Validuje JSON text proti JSON Schema.
    /// </summary>
    /// <param name="jsonText">Text vstupního JSONu.</param>
    /// <param name="schemaText">Text JSON schématu.</param>
    /// <returns>Seznam chyb, nebo prázdný list, pokud je JSON validní.</returns>
    public static List<ErrorItem> ValidateJson(string jsonText, string schemaText)
    {
      var errors = new List<ErrorItem>();

      try
      {
        // Načtení schématu
        JSchema schema = JSchema.Parse(schemaText);

        // Parse JSONu (může být objekt, pole nebo cokoliv)
        var token = JToken.Parse(jsonText);

        // Validace se sběrem všech chyb
        bool valid = token.IsValid(schema, out IList<Newtonsoft.Json.Schema.ValidationError> validationErrors);

        if (!valid && validationErrors != null)
        {
          List<string> tmp = new List<string>();
          CollectErrors(validationErrors, tmp);
          foreach (var errMsg in tmp)
          {
            errors.Add(ConvertExceptionToErrorItem("JSON-in", errMsg));
          }
        }
      }
      catch (JsonReaderException ex)
      {
        errors.Add(ConvertExceptionToErrorItem("JSON", ex.Message));
      }
      catch (JSchemaException ex)
      {
        errors.Add(ConvertExceptionToErrorItem("JSON-schema", ex.Message));
      }

      return errors;
    }

    private static void CollectErrors(IEnumerable<Newtonsoft.Json.Schema.ValidationError> validationErrors, List<string> output)
    {
      foreach (var err in validationErrors)
      {
        string path = string.IsNullOrWhiteSpace(err.Path) ? "(root)" : err.Path;
        output.Add($"{err.Message} Path '{err.Path}', line {err.LineNumber}, position {err.LinePosition}.");

        // Vnořené chyby (např. u objektů nebo polí)
        if (err.ChildErrors != null && err.ChildErrors.Count > 0)
          CollectErrors(err.ChildErrors, output);
      }
    }

    private static ErrorItem ConvertExceptionToErrorItem(string type, string erroMessage)
    {
      int pathIndex = erroMessage.IndexOf(" Path '");
      if (pathIndex != -1)
      {
        string message = erroMessage.Substring(0, pathIndex);
        string location = erroMessage.Substring(pathIndex);
        return new ErrorItem(type, message, location);
      }
      else
      {
        return new ErrorItem(type, erroMessage, "");
      }
    }

    private void btnDescription_Click(object sender, RoutedEventArgs e)
    {
      const string url = "https://github.com/Engin1980/fs2020-com-to-vpilot-volume/wiki/Configuration-File";

      try
      {
        var psi = new ProcessStartInfo(url)
        {
          UseShellExecute = true
        };
        Process.Start(psi);
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this, $"Unable to open the help page:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void btnSchema_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        string schemaPath = System.IO.Path.GetFullPath("appsettings.schema.json");
        if (!System.IO.File.Exists(schemaPath))
        {
          System.Windows.MessageBox.Show(this, $"Schema file not found:\n{schemaPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{schemaPath}\"") { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this, $"Unable to open schema file in Explorer:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void btnReload_Click(object sender, RoutedEventArgs e)
    {
      var result = System.Windows.MessageBox.Show(this, "Reload will discard unsaved changes. Do you want to continue?", "Confirm reload", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
      {
        try
        {
          LoadUserJsonFile();
        }
        catch (Exception ex)
        {
          System.Windows.MessageBox.Show(this, $"Unable to reload user JSON file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private void btnDiscard_Click(object sender, RoutedEventArgs e)
    {
      var result = System.Windows.MessageBox.Show(this, "Discard changes and close the editor?", "Confirm discard", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
      {
        this.Close();
      }
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
      var resultConfirm = System.Windows.MessageBox.Show(this, "Are you sure you want to save the changes to the user config file? This will overwrite the existing file.", "Confirm save", MessageBoxButton.YesNo, MessageBoxImage.Warning);
      if (resultConfirm != MessageBoxResult.Yes)
      {
        return;
      }

      string file = Eng.Com2vPilotVolume.Types.SettingsProvider.UserConfigFilePath;
      try
      {
        System.IO.File.WriteAllText(file, txtJson.Text, Encoding.UTF8);
        System.Windows.MessageBox.Show(this, $"User config file saved successfully:\n{file}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this, $"Unable to save user config file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var result = System.Windows.MessageBox.Show(this, "Settings saved. A hard restart of the application is required to apply the changes. Do you want to close the application now?", "Restart required", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
      {
        Process.Start(System.Windows.Application.ResourceAssembly.Location);
        System.Windows.Application.Current.Shutdown();
      }
    }


    private void btnUserFile_Click(object sender, RoutedEventArgs e)
    {
      string file = Eng.Com2vPilotVolume.Types.SettingsProvider.UserConfigFilePath;
      try
      {
        if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file))
        {
          System.Windows.MessageBox.Show(this, $"User config file not found:\n{file}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        var psi = new ProcessStartInfo("explorer.exe", $"/select,\"{System.IO.Path.GetFullPath(file)}\"") { UseShellExecute = true };
        Process.Start(psi);
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this, $"Unable to open user config file in Explorer:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void btnDefaultFile_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        string defaultPath = System.IO.Path.GetFullPath("appSettings.json");
        if (!System.IO.File.Exists(defaultPath))
        {
          System.Windows.MessageBox.Show(this, $"Default applications file not found:\n{defaultPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{defaultPath}\"") { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show(this, $"Unable to open default file in Explorer:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void txtJson_TextChanged(object sender, EventArgs e)
    {
      ProcessTextChanged();
    }
  }
}
