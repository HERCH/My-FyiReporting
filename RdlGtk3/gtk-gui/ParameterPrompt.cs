
// This file has been generated by the GUI designer. Do not modify.
namespace Majorsilence.Reporting.RdlGtk3
{
    public partial class ParameterPrompt : Gtk.Dialog
    {
        private Gtk.HBox hbox3;
        private Gtk.Label label2;
        private Gtk.Entry TextBoxParameters;
        private Gtk.Button buttonCancel;
        private Gtk.Button buttonOk;

        protected virtual void Build()
        {
            // Widget Majorsilence.Reporting.RdlGtkViewer.ParameterPrompt
            this.Name = "fyiReporting.RdlGtk3.ParameterPrompt";
            this.WindowPosition = Gtk.WindowPosition.Center;

            // Internal child Majorsilence.Reporting.RdlGtkViewer.ParameterPrompt.VBox
#pragma warning disable CS0612 // Type or member is obsolete
            Gtk.VBox vbox = new Gtk.VBox();
#pragma warning restore CS0612 // Type or member is obsolete
            vbox.Name = "dialog1_VBox";
            vbox.BorderWidth = 2;

            // Container child dialog1_VBox.Gtk.Box+BoxChild
#pragma warning disable CS0612 // Type or member is obsolete
            this.hbox3 = new Gtk.HBox();
#pragma warning restore CS0612 // Type or member is obsolete
            this.hbox3.Name = "hbox3";
            this.hbox3.Spacing = 6;

            // Container child hbox3.Gtk.Box+BoxChild
            this.label2 = new Gtk.Label();
            this.label2.Name = "label2";
            this.label2.LabelProp = "Enter Parameters";
            this.hbox3.PackStart(this.label2, false, false, 0);

            // Container child hbox3.Gtk.Box+BoxChild
            this.TextBoxParameters = new Gtk.Entry();
            this.TextBoxParameters.CanFocus = true;
            this.TextBoxParameters.Name = "TextBoxParameters";
            this.TextBoxParameters.IsEditable = true;
            this.TextBoxParameters.InvisibleChar = '•';
            this.hbox3.PackStart(this.TextBoxParameters, false, false, 0);

            vbox.PackStart(this.hbox3, false, false, 0);

            // Internal child Majorsilence.Reporting.RdlGtkViewer.ParameterPrompt.ActionArea
#pragma warning disable CS0612 // Type or member is obsolete
            Gtk.HButtonBox actionArea = new Gtk.HButtonBox();
#pragma warning restore CS0612 // Type or member is obsolete
            actionArea.Name = "dialog1_ActionArea";
            actionArea.Spacing = 10;
            actionArea.BorderWidth = 5;
            actionArea.LayoutStyle = Gtk.ButtonBoxStyle.End;

            // Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.buttonCancel = new Gtk.Button();
            this.buttonCancel.CanDefault = true;
            this.buttonCancel.CanFocus = true;
            this.buttonCancel.Name = "buttonCancel";
#pragma warning disable CS0612 // Type or member is obsolete
            this.buttonCancel.UseStock = true;
#pragma warning restore CS0612 // Type or member is obsolete
            this.buttonCancel.UseUnderline = true;
            this.buttonCancel.Label = "gtk-cancel";
            actionArea.PackStart(this.buttonCancel, false, false, 0);

            // Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
            this.buttonOk = new Gtk.Button();
            this.buttonOk.CanDefault = true;
            this.buttonOk.CanFocus = true;
            this.buttonOk.Name = "buttonOk";
#pragma warning disable CS0612 // Type or member is obsolete
            this.buttonOk.UseStock = true;
#pragma warning restore CS0612 // Type or member is obsolete
            this.buttonOk.UseUnderline = true;
            this.buttonOk.Label = "gtk-ok";
            actionArea.PackStart(this.buttonOk, false, false, 0);

            vbox.PackStart(actionArea, false, false, 0);

            this.Add(vbox);

            this.DefaultWidth = 400;
            this.DefaultHeight = 237;
            this.ShowAll();
        }
    }
}
