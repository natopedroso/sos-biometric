using System;
using System.Collections.Generic;
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
      string userId;
      if (!TryGetArg(args, "--user-id", out userId) || !IsValidUserId(userId)) {
        return new CliResult {
          Ok = false,
          Status = "error",
          Message = "Parametro --user-id obrigatorio e deve ser numerico."
        };
      }

      switch (command) {
        case "enroll":
          return RunEnroll(userId);
        case "verify":
          return RunVerify(userId);
        default:
          return new CliResult {
            Ok = false,
            Status = "error",
            Message = "Comando invalido. Use enroll ou verify."
          };
      }
    }

    private static CliResult RunEnroll(string userId) {
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
        DPFP.Template template = data.Templates[0];
        if (template == null) return;

        string saveError;
        bool saved = BiometricTemplateStore.Save(userId, template, out saveError);
        if (!saved) {
          result.Ok = false;
          result.Status = "error";
          result.Message = saveError;
        } else {
          result.Ok = true;
          result.Status = "success";
          result.Message = "Digital cadastrada com sucesso.";
        }

        form.BeginInvoke(new MethodInvoker(delegate { form.Close(); }));
      };

      Application.Run(form);
      return result;
    }

    private static CliResult RunVerify(string userId) {
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