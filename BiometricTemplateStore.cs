using System;
using System.IO;

namespace UI_Support {
  internal static class BiometricTemplateStore {
    private const string BaseDirectory = @"C:\\sos-biometric\\data\\templates";

    public static bool TryLoad(string userId, out DPFP.Template template, out string error) {
      template = null;
      error = null;

      try {
        string path = GetTemplatePath(userId);
        if (!File.Exists(path)) {
          error = "Template biometrico nao encontrado para o usuario.";
          return false;
        }

        byte[] bytes = File.ReadAllBytes(path);
        using (MemoryStream ms = new MemoryStream(bytes)) {
          DPFP.Template loaded = new DPFP.Template();
          loaded.DeSerialize(ms);
          template = loaded;
        }

        return true;
      } catch (Exception ex) {
        error = "Falha ao carregar template biometrico: " + ex.Message;
        return false;
      }
    }

    public static bool Save(string userId, DPFP.Template template, out string error) {
      error = null;
      if (template == null) {
        error = "Template biometrico invalido.";
        return false;
      }

      try {
        Directory.CreateDirectory(BaseDirectory);
        string path = GetTemplatePath(userId);

        using (MemoryStream ms = new MemoryStream()) {
          template.Serialize(ms);
          File.WriteAllBytes(path, ms.ToArray());
        }

        return true;
      } catch (Exception ex) {
        error = "Falha ao salvar template biometrico: " + ex.Message;
        return false;
      }
    }

    private static string GetTemplatePath(string userId) {
      return Path.Combine(BaseDirectory, "user-" + userId + ".fpt");
    }
  }
}
