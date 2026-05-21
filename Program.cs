using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UI_Support {
  static class Program {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args) {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      if (args != null && args.Length > 0) {
        CliResult result = RunCli(args);
        Console.WriteLine(result.ToJson());
        return result.ExitCode;
      }

      Application.Run(new MainForm());
      return 0;
    }

    private static CliResult RunCli(string[] args) {
      string command = args[0] != null ? args[0].Trim().ToLowerInvariant() : "";
      switch (command) {
        case "enroll":
          string enrollUserId;
          if (!TryGetArg(args, "--user-id", out enrollUserId) || !IsValidUserId(enrollUserId)) {
            return new CliResult {
              Ok = false,
              Status = "error",
              Message = "Parametro --user-id obrigatorio e deve ser numerico."
            };
          }
          return RunEnroll(enrollUserId);

        case "verify":
          string verifyUserId;
          if (!TryGetArg(args, "--user-id", out verifyUserId) || !IsValidUserId(verifyUserId)) {
            return new CliResult {
              Ok = false,
              Status = "error",
              Message = "Parametro --user-id obrigatorio e deve ser numerico."
            };
          }
          return RunVerify(verifyUserId);

        case "identify":
          return RunIdentify();

        default:
          return new CliResult {
            Ok = false,
            Status = "error",
            Message = "Comando invalido. Use enroll, verify ou identify."
          };
      }
    }

    private static CliResult RunEnroll(string userId) {
      MessageBox.Show(
        "Cadastro biometrico iniciado. Posicione o dedo no leitor quando solicitado e conclua as leituras.",
        "Instrucoes - Cadastro Biometrico",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
      );

      AppData data = new AppData();
      data.MaxEnrollFingerCount = 1;
      data.EnrolledFingersMask = 0;
      data.IsEventHandlerSucceeds = true;

      EnrollmentForm form = new EnrollmentForm(data);
      CliResult result = new CliResult {
        Ok = false,
        Status = "cancelled",
        Message = "Cadastro biometrico cancelado."
      };

      data.OnChange += delegate {
        if (result.Ok) return;
        DPFP.Template template;
        int finger;
        if (!TryGetAnyEnrolledTemplate(data, out template, out finger)) return;

        string saveError;
        bool saved = BiometricTemplateStore.Save(userId, template, out saveError);
        if (!saved) {
          result.Ok = false;
          result.Status = "error";
          result.Message = saveError;
        } else {
          result.Ok = true;
          result.Status = "success";
          result.Message = "Digital cadastrada com sucesso (dedo " + finger + ").";
        }

        form.BeginInvoke(new MethodInvoker(delegate { form.Close(); }));
      };

      Application.Run(form);
      return result;
    }

    private static bool TryGetAnyEnrolledTemplate(AppData data, out DPFP.Template template, out int finger) {
      template = null;
      finger = 0;
      if (data == null || data.Templates == null) return false;

      for (int i = 0; i < data.Templates.Length; i++) {
        if (data.Templates[i] == null) continue;
        template = data.Templates[i];
        finger = i + 1;
        return true;
      }

      return false;
    }

    private static CliResult RunVerify(string userId) {
      MessageBox.Show(
        "Validacao biometrica iniciada. Posicione o dedo no leitor quando solicitado.",
        "Instrucoes - Login Biometrico",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information
      );

      DPFP.Template template;
      string loadError;
      bool loaded = BiometricTemplateStore.TryLoad(userId, out template, out loadError);
      if (!loaded) {
        return new CliResult {
          Ok = false,
          Status = "failed",
          Message = loadError
        };
      }

      AppData data = new AppData();
      data.MaxEnrollFingerCount = 1;
      data.EnrolledFingersMask = 1;
      data.IsEventHandlerSucceeds = true;
      data.Templates[0] = template;

      VerificationForm form = new VerificationForm(data);
      CliResult result = new CliResult {
        Ok = false,
        Status = "cancelled",
        Message = "Validacao biometrica cancelada."
      };

      data.OnChange += delegate {
        if (!data.IsFeatureSetMatched || result.Ok) return;

        result.Ok = true;
        result.Status = "success";
        result.Message = "Digital validada com sucesso.";
        form.BeginInvoke(new MethodInvoker(delegate { form.Close(); }));
      };

      Application.Run(form);
      return result;
    }

    private static CliResult RunIdentify() {
      List<DPFP.Template> templates;
      List<int> userIds;
      string loadError;
      if (!TryLoadAllTemplates(out templates, out userIds, out loadError)) {
        return new CliResult {
          Ok = false,
          Status = "failed",
          Message = loadError
        };
      }

      AppData data = new AppData();
      data.MaxEnrollFingerCount = 1;
      data.EnrolledFingersMask = templates.Count;
      data.IsEventHandlerSucceeds = true;
      data.Templates = templates.ToArray();

      VerificationForm form = new VerificationForm(data);
      CliResult result = new CliResult {
        Ok = false,
        Status = "cancelled",
        Message = "Identificacao biometrica cancelada."
      };

      data.OnChange += delegate {
        if (!data.IsFeatureSetMatched || result.Ok) return;

        int idx = data.MatchedTemplateIndex;
        if (idx < 0 || idx >= userIds.Count) return;

        result.Ok = true;
        result.Status = "success";
        result.UserId = userIds[idx];
        result.Message = "Digital identificada com sucesso.";
        form.BeginInvoke(new MethodInvoker(delegate { form.Close(); }));
      };

      Application.Run(form);
      return result;
    }

    private static bool TryLoadAllTemplates(
      out List<DPFP.Template> templates,
      out List<int> userIds,
      out string error
    ) {
      templates = new List<DPFP.Template>();
      userIds = new List<int>();
      error = null;

      string templatesDir = @"C:\sos-biometric\data\templates";
      if (!Directory.Exists(templatesDir)) {
        error = "Nenhum template biometrico encontrado para identificacao.";
        return false;
      }

      Regex pattern = new Regex(@"^user-(\d+)\.fpt$", RegexOptions.IgnoreCase);
      string[] files = Directory.GetFiles(templatesDir, "user-*.fpt")
        .OrderBy(p => p)
        .ToArray();

      foreach (string filePath in files) {
        string fileName = Path.GetFileName(filePath);
        Match match = pattern.Match(fileName);
        if (!match.Success) continue;

        int parsedUserId;
        if (!int.TryParse(match.Groups[1].Value, out parsedUserId)) continue;

        DPFP.Template template;
        string loadError;
        bool loaded = BiometricTemplateStore.TryLoad(parsedUserId.ToString(), out template, out loadError);
        if (!loaded || template == null) continue;

        userIds.Add(parsedUserId);
        templates.Add(template);
      }

      if (templates.Count == 0) {
        error = "Nenhum template biometrico valido encontrado para identificacao.";
        return false;
      }

      return true;
    }

    private static bool TryGetArg(string[] args, string key, out string value) {
      value = null;
      for (int i = 0; i < args.Length - 1; i++) {
        if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) continue;
        value = args[i + 1];
        return true;
      }

      return false;
    }

    private static bool IsValidUserId(string userId) {
      if (string.IsNullOrEmpty(userId)) return false;
      for (int i = 0; i < userId.Length; i++) {
        if (!char.IsDigit(userId[i])) return false;
      }

      return true;
    }
  }
}