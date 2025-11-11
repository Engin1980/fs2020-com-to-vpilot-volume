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

namespace eng.com2vPilotVolume
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
      var json = System.IO.File.ReadAllText("appsettings.json");
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
          List<string> tmp = [];
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
        output.Add($"{err.Message} Path '${err.Path}', line ${err.LineNumber}, position {err.LinePosition}.");

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
      const string url = "https://github.com/Engin1980/VPilotNotifications/wiki/Configuration-description-%E2%80%90-AppSettings.json";

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
      // open file "applications.schema.json" in explorer
    }

    private void btnReload_Click(object sender, RoutedEventArgs e)
    {
      // ask for confirmation, then use LoadUserJsonFile();
    }

    private void btnDiscard_Click(object sender, RoutedEventArgs e)
    {
      // ask for confirmation, then close this window
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
      //do nothing
    }


    private void btnUserFile_Click(object sender, RoutedEventArgs e)
    {
      string file = eng.com2vPilotVolume.Types.SettingsProvider.UserConfigFilePath;
      // open file in explorer
    }

    private void btnDefaultFile_Click(object sender, RoutedEventArgs e)
    {
      // open file "applications.json" in explorer
    }

    private void txtJson_TextChanged(object sender, EventArgs e)
    {
      ProcessTextChanged();
    }
  }
}
