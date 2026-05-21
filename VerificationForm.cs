using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UI_Support {
  public partial class VerificationForm : Form {
    public VerificationForm(AppData data) {
      InitializeComponent();
      Data = data;
    }

    public void OnComplete(object Control, DPFP.FeatureSet FeatureSet, ref DPFP.Gui.EventHandlerStatus Status) {
      DPFP.Verification.Verification ver = new DPFP.Verification.Verification();
      DPFP.Verification.Verification.Result res = new DPFP.Verification.Verification.Result();
      Data.IsFeatureSetMatched = false;
      Data.MatchedTemplateIndex = -1;

      // Compare feature set with all stored templates.
      for (int i = 0; i < Data.Templates.Length; i++) {
        DPFP.Template template = Data.Templates[i];
        // Get template from storage.
        if (template != null) {
          // Compare feature set with particular template.
          ver.Verify(FeatureSet, template, ref res);
          Data.IsFeatureSetMatched = res.Verified;
          Data.FalseAcceptRate = res.FARAchieved;
          if (res.Verified) {
            Data.MatchedTemplateIndex = i;
            break; // success
          }
        }
      }

      if (!res.Verified)
        Status = DPFP.Gui.EventHandlerStatus.Failure;

      Data.Update();
    }

    private AppData Data;
  }
}