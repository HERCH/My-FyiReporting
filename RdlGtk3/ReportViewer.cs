// 
//  ReportViewer.cs
//  
//  Author:
//       Krzysztof Marecki
// 
//  Copyright (c) 2010 Krzysztof Marecki
//  Copyright (c) 2012 Peter Gill
// 
// This file is part of the NReports project
// This file is part of the My-FyiReporting project 
//	
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Gtk;
using System.Threading.Tasks;
using Strings = Majorsilence.Reporting.RdlEngine.Resources.Strings;

namespace Majorsilence.Reporting.RdlGtk3
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class ReportViewer : Gtk.Bin
    {
        private Report report;
        private Pages pages;
        private PrintOperation printing;

        public NeedPassword DataSourceReferencePassword = null;

        private string connectionString;
        private bool overwriteSubreportConnection;
        private OutputPresentationType[] restrictedOutputPresentationTypes;
        private Action<Pages> customPrintAction;

        public event EventHandler ReportPrinted;

        private Toolbar actionGroup { get; set; }
        private Gtk.Action refreshAction;
        private Gtk.Action saveAsAction;
        private Gtk.Action printAction;
        private Gtk.Action zoomOutAction;
        private Gtk.Action zoomInAction;
        private Gtk.ToggleAction errorsAction;
        private Gtk.Box vbox;
        private Gtk.Box hbox;
        private Gtk.Box vboxParameters;
        private Gtk.Paned hpanedReport;
        private Gtk.ScrolledWindow scrolledwindowPages;
        private Gtk.Box vboxPages;
        private Gtk.ScrolledWindow scrolledwindowErrors;
        private Gtk.TextView textviewErrors;

        public ListDictionary Parameters { get; private set; }

        bool show_errors;

        public bool ShowErrors
        {
            get
            {
                return show_errors;
            }
            set
            {
                show_errors = value;
                CheckVisibility();
            }
        }

        public string DefaultExportFileName { get; set; }

        bool show_params;

        public bool ShowParameters
        {
            get
            {
                return show_params;
            }
            set
            {
                show_params = value;
                CheckVisibility();
            }
        }

        public string WorkingDirectory { get; set; }

        Uri sourceFile;
        public Uri SourceFile
        {
            get
            {
                return sourceFile;
            }
            private set
            {
                sourceFile = value;
                WorkingDirectory = System.IO.Path.GetDirectoryName(sourceFile.LocalPath);
            }
        }

        public ReportViewer()
        {
            Build();
            Parameters = new ListDictionary();

            errorsAction.Toggled += OnErrorsActionToggled;
            DisableActions();
            ShowErrors = false;
        }

        protected virtual void Build()
        {
            actionGroup = new Toolbar();

#pragma warning disable CS0612 // Type or member is obsolete
            refreshAction = new Gtk.Action("refresh", "Refresh", "gtk-refresh", Stock.Refresh);
            refreshAction.IsImportant = true;
            refreshAction.Tooltip = "Refresh the report";
            actionGroup.Add(refreshAction.CreateToolItem());

            saveAsAction = new Gtk.Action("export", "Export", "gtk-save-as", Stock.SaveAs);
            saveAsAction.Tooltip = "Export as PDF, CSV, ASP, HTML, MHTML, XML, Excel";
            actionGroup.Add(saveAsAction.CreateToolItem());

            printAction = new Gtk.Action("print", "Print", "gtk-print", Stock.Print);
            printAction.Tooltip = "Print the report";
            actionGroup.Add(printAction.CreateToolItem());

            zoomOutAction = new Gtk.Action("zoom-out", "Zoom Out", "gtk-zoom-out", Stock.ZoomOut);

            zoomOutAction.IsImportant = true;

            zoomOutAction.Tooltip = "Zoom out the report";
            actionGroup.Add(zoomOutAction.CreateToolItem());

            zoomInAction = new Gtk.Action("zoom-in", "Zoom In", "gtk-zoom-in", Stock.ZoomIn);
            zoomInAction.IsImportant = true;
            zoomInAction.Tooltip = "Zoom in the report";
            actionGroup.Add(zoomInAction.CreateToolItem());

            errorsAction = new ToggleAction(null, null, "gtk-dialog-warning", Stock.DialogWarning);
            errorsAction.IsImportant = true;
            errorsAction.DrawAsRadio = true;
            actionGroup.Add(errorsAction.CreateToolItem());

            vbox = new Box(Orientation.Vertical, 6);
            vbox.Homogeneous = false;
            vbox.Expand = true;


            hbox = new Box(Orientation.Horizontal, 6);
            hbox.Homogeneous = false;
            hbox.Expand = true;
            vboxParameters = new Box(Orientation.Horizontal, 6);
            vboxParameters.Homogeneous = false;
            hbox.PackStart(vboxParameters, false, false, 0);
            vbox.Add(actionGroup);

            hpanedReport = new Paned(Orientation.Horizontal);
            hpanedReport.Expand = true;
            scrolledwindowPages = new ScrolledWindow();
            scrolledwindowPages.Expand = true;

            var gtkViewport = new Viewport();
            gtkViewport.Expand = true;
            vboxPages = new Box(Orientation.Vertical, 6);
            vboxPages.Homogeneous = false;
            vboxPages.Expand = true;
            gtkViewport.Add(vboxPages);
            scrolledwindowPages.Add(gtkViewport);
            hpanedReport.Pack1(scrolledwindowPages, true, true);

            scrolledwindowErrors = new ScrolledWindow();
            textviewErrors = new TextView();
            textviewErrors.WidthRequest = 200;
            textviewErrors.Editable = false;
            textviewErrors.WrapMode = WrapMode.WordChar;
            scrolledwindowErrors.Add(textviewErrors);
            hpanedReport.Pack2(scrolledwindowErrors, true, true);

            hbox.PackEnd(hpanedReport, true, true, 0);
            vbox.PackEnd(hbox, true, true, 0);

            Add(vbox);
            vbox.ShowAll();

            this.refreshAction.Activated += new global::System.EventHandler(this.OnRefreshActionActivated);
            this.saveAsAction.Activated += new global::System.EventHandler(this.OnPdfActionActivated);
            this.printAction.Activated += new global::System.EventHandler(this.OnPrintActionActivated);
            this.zoomOutAction.Activated += new global::System.EventHandler(this.OnZoomOutActionActivated);
            this.zoomInAction.Activated += new global::System.EventHandler(this.OnZoomInActionActivated);
            this.hpanedReport.SizeAllocated += new global::Gtk.SizeAllocatedHandler(this.OnHpanedReportSizeAllocated);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="connectionString">Relace all Connection string in report.</param>
        /// <param name="overwriteSubreportConnection">If true connection string in subreport also will be overwrite</param>
        /// <param name="restrictedOutputPresentationTypes">Restricts <see cref="OutputPresentationType"/> to chose from in export dialog</param>
        /// <param name="customPrintAction">>For use a custom print action</param>
        public async Task LoadReport(Uri filename, string parameters, string connectionString, bool overwriteSubreportConnection = false, OutputPresentationType[] restrictedOutputPresentationTypes = null, Action<Pages> customPrintAction = null)
        {
            SourceFile = filename;

            this.connectionString = connectionString;
            this.overwriteSubreportConnection = overwriteSubreportConnection;
            this.customPrintAction = customPrintAction;

            await LoadReport(filename, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        /// <param name="source">Xml source of report</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="connectionString">Relace all Connection string in report.</param>
        /// <param name="overwriteSubreportConnection">If true connection string in subreport also will be overwrite</param>
        /// <param name="restrictedOutputPresentationTypes">Restricts <see cref="OutputPresentationType"/> to chose from in export dialog</param>
        public async Task LoadReport(string source, string parameters, string connectionString, bool overwriteSubreportConnection = false, OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            this.connectionString = connectionString;
            this.overwriteSubreportConnection = overwriteSubreportConnection;

            await LoadReport(source, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        /// <param name='filename'>Filename.</param>
        /// <param name="restrictedOutputPresentationTypes">Restricts <see cref="OutputPresentationType"/> to chose from in export dialog</param>
        public async Task LoadReport(Uri filename, OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            await LoadReport(filename, "", restrictedOutputPresentationTypes);
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        /// <param name='sourcefile'>Filename.</param>
        /// <param name='parameters'>Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="restrictedOutputPresentationTypes">Restricts <see cref="OutputPresentationType"/> to chose from in export dialog</param>
        public async Task LoadReport(Uri sourcefile, string parameters, OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            SourceFile = sourcefile;

            string source = System.IO.File.ReadAllText(sourcefile.LocalPath);
            await LoadReport(source, parameters, restrictedOutputPresentationTypes);
        }

        /// <summary>
        /// Loads the report.
        /// </summary>
        /// <param name="source">Xml source of report</param>
        /// <param name="parameters">Example: parameter1=someValue&parameter2=anotherValue</param>
        /// <param name="restrictedOutputPresentationTypes">Restricts <see cref="OutputPresentationType"/> to chose from in export dialog</param>
        public async Task LoadReport(string source, string parameters, OutputPresentationType[] restrictedOutputPresentationTypes = null)
        {
            this.restrictedOutputPresentationTypes = restrictedOutputPresentationTypes ?? new OutputPresentationType[0];

            // Any parameters?  e.g.  file1.rdl?orderid=5 
            if (parameters.Trim() != "")
                this.Parameters = this.GetParmeters(parameters);
            else
                this.Parameters = new ListDictionary();

            // Compile the report 
            report = await this.GetReport(source);
            if (report == null)
                return;
            AddParameterControls();

            await RefreshReport();
        }

        async Task RefreshReport()
        {
            if (ShowParameters)
                SetParametersFromControls();
            await report.RunGetData(Parameters);
            pages = await report.BuildPages();

            foreach (Gtk.Widget w in vboxPages.AllChildren)
            {
                vboxPages.Remove(w);
            }

            for (int pageCount = 0; pageCount < pages.Count; pageCount++)
            {
                ReportArea area = new ReportArea();
                area.SetReport(report, pages[pageCount]);
                //area.Scale
                vboxPages.Add(area);
            }
            this.ShowAll();

            SetErrorMessages(report.ErrorItems);

            if (report.ErrorMaxSeverity == 0)
                show_errors = false;

#pragma warning disable CS0612 // Type or member is obsolete
            errorsAction.VisibleHorizontal = report.ErrorMaxSeverity > 0;
#pragma warning restore CS0612 // Type or member is obsolete

            //			Title = string.Format ("RDL report viewer - {0}", report.Name);
            EnableActions();
            CheckVisibility();
        }
        
        protected void OnZoomOutActionActivated(object sender, System.EventArgs e)
        {
            foreach (Gtk.Widget w in vboxPages.AllChildren)
            {
                if (w is ReportArea)
                {
                    ((ReportArea)w).Scale -= 0.1f;
                }
            }
            //reportarea.Scale -= 0.1f;
        }

        protected void OnZoomInActionActivated(object sender, System.EventArgs e)
        {
            foreach (Gtk.Widget w in vboxPages.AllChildren)
            {
                if (w is ReportArea)
                {
                    ((ReportArea)w).Scale += 0.1f;
                }
            }
            //reportarea.Scale += 0.1f;
        }

        // GetParameters creates a list dictionary
        // consisting of a report parameter name and a value.
        private System.Collections.Specialized.ListDictionary GetParmeters(string parms)
        {
            System.Collections.Specialized.ListDictionary ld = new System.Collections.Specialized.ListDictionary();
            if (parms == null)
            {
                return ld; // dictionary will be empty in this case
            }

            // parms are separated by & 

            char[] breakChars = new char[] { '&' };
            string[] ps = parms.Split(breakChars);

            foreach (string p in ps)
            {
                int iEq = p.IndexOf("=");
                if (iEq > 0)
                {
                    string name = p.Substring(0, iEq);
                    string val = p.Substring(iEq + 1);
                    ld.Add(name, val);
                }
            }
            return ld;
        }

        private async Task<Report> GetReport(string reportSource)
        {
            // Now parse the file 

            RDLParser rdlp;
            Report r;

            rdlp = new RDLParser(reportSource);
            rdlp.Folder = WorkingDirectory;
            rdlp.OverwriteConnectionString = connectionString;
            rdlp.OverwriteInSubreport = overwriteSubreportConnection;
            // RDLParser takes RDL XML and Parse compiles the report

            r = await rdlp.Parse();
            if (r.ErrorMaxSeverity > 0)
            {
                foreach (string emsg in r.ErrorItems)
                {
                    Console.WriteLine(emsg);
                }
                SetErrorMessages(r.ErrorItems);

                int severity = r.ErrorMaxSeverity;
                r.ErrorReset();
                if (severity > 4)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    errorsAction.Active = true;
#pragma warning restore CS0612 // Type or member is obsolete
                    return null; // don't return when severe errors
                }
            }
            return r;
        }


        void OnErrorsActionToggled(object sender, EventArgs e)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            ShowErrors = errorsAction.Active;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        void CheckVisibility()
        {
            if (ShowErrors)
            {
                scrolledwindowErrors.ShowAll();
            }
            else
            {
                //scrolledwindowErrors.HideAll();
            }
            if (ShowParameters)
            {
                vboxParameters.ShowAll();
            }
            else
            {
                //vboxParameters.HideAll();
            }
        }

        protected override void OnShown()
        {
            base.OnShown();
            CheckVisibility();
        }

        void DisableActions()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            saveAsAction.Sensitive = false;
            refreshAction.Sensitive = false;
            printAction.Sensitive = false;
            zoomInAction.Sensitive = false;
            zoomOutAction.Sensitive = false;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        void EnableActions()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            saveAsAction.Sensitive = true;
            refreshAction.Sensitive = true;
            printAction.Sensitive = true;
            zoomInAction.Sensitive = true;
            zoomOutAction.Sensitive = true;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        void AddParameterControls()
        {
            foreach (Widget child in vboxParameters.Children)
            {
                vboxParameters.Remove(child);
            }
            foreach (UserReportParameter rp in report.UserReportParameters)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                HBox hbox = new HBox();
#pragma warning restore CS0612 // Type or member is obsolete
                Label labelPrompt = new Label();
#pragma warning disable CS0612 // Type or member is obsolete
                labelPrompt.SetAlignment(0, 0.5f);
#pragma warning restore CS0612 // Type or member is obsolete
                labelPrompt.Text = string.Format("{0} :", rp.Prompt);
                hbox.PackStart(labelPrompt, true, true, 0);
                Entry entryValue = new Entry();
                if (Parameters.Contains(rp.Name))
                {
                    if (Parameters[rp.Name] != null)
                    {
                        entryValue.Text = Parameters[rp.Name].ToString();
                    }
                }
                else
                {
                    if (rp.DefaultValue != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < rp.DefaultValue.Length; i++)
                        {
                            if (i > 0)
                                sb.Append(", ");
                            sb.Append(rp.DefaultValue[i].ToString());
                        }
                        entryValue.Text = sb.ToString();
                    }
                }
                hbox.PackStart(entryValue, false, false, 0);
                vboxParameters.PackStart(hbox, false, false, 0);
            }
        }

        void SetParametersFromControls()
        {
            int i = 0;
            foreach (UserReportParameter rp in report.UserReportParameters)
            {
                HBox hbox = (HBox)vboxParameters.Children[i];
                Entry entry = (Entry)hbox.Children[1];
                //parameters.Add (rp.Name, entry.Text);
                Parameters[rp.Name] = entry.Text;
                i++;
            }
        }

        void SetErrorMessages(IList errors)
        {
            textviewErrors.Buffer.Clear();

            if (errors == null || errors.Count == 0)
                return;

            StringBuilder msgs = new StringBuilder();
            msgs.AppendLine("Report rendering errors:");
            msgs.AppendLine();
            foreach (var error in errors)
                msgs.AppendLine(error.ToString());

            textviewErrors.Buffer.Text = msgs.ToString();
        }

        protected async void OnPdfActionActivated(object sender, System.EventArgs e)
        {
            await SaveReport();
        }

        public async Task SaveReport()
        {
            // *********************************
            object[] param = new object[4];
            param[0] = Strings.ButtonCancel_Text;
            param[1] = Gtk.ResponseType.Cancel;
            param[2] = Strings.ButtonSave_Text;
            param[3] = Gtk.ResponseType.Accept;

            using var fc =
                new Gtk.FileChooserDialog(Strings.FileChooser_SaveFileTo_Title,
                    null,
                    Gtk.FileChooserAction.Save,
                    param);

            fc.CurrentName = DefaultExportFileName ?? report.Name;

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.PDF))
            {
                Gtk.FileFilter pdfFilter = new Gtk.FileFilter { Name = "PDF" };
                var extensionPDF = ".pdf";
                pdfFilter.AddPattern($"*{extensionPDF}");
                fc.AddFilter(pdfFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.CSV))
            {
                Gtk.FileFilter csvFilter = new Gtk.FileFilter { Name = "CSV" };
                var extensionCSV = ".csv";
                csvFilter.AddPattern($"*{extensionCSV}");
                fc.AddFilter(csvFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.Excel2007DataOnly))
            {
                Gtk.FileFilter excel2007Data = new Gtk.FileFilter { Name = "Excel no formatting" };
                var extensionXLSX = ".xlsx";
                excel2007Data.AddPattern($"*{extensionXLSX}");
                fc.AddFilter(excel2007Data);
            }
            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.Excel2007))
            {
                Gtk.FileFilter excel2007 = new Gtk.FileFilter { Name = "Excel with formatting" };
                var extensionXLSX = ".xlsx";
                excel2007.AddPattern($"*{extensionXLSX}");
                fc.AddFilter(excel2007);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.TIF))
            {
                Gtk.FileFilter tiffFilter = new Gtk.FileFilter { Name = "TIFF" };
                var extensionTIFF = ".tiff";
                tiffFilter.AddPattern($"*{extensionTIFF}");
                fc.AddFilter(tiffFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.ASPHTML))
            {
                Gtk.FileFilter asphtmlFilter = new Gtk.FileFilter { Name = "ASP HTML" };
                var extensionASPHTML = ".asphtml";
                asphtmlFilter.AddPattern($"*{extensionASPHTML}");
                fc.AddFilter(asphtmlFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.HTML))
            {
                Gtk.FileFilter htmlFilter = new Gtk.FileFilter { Name = "HTML" };
                var extensionHTML = ".html";
                htmlFilter.AddPattern($"*{extensionHTML}");
                fc.AddFilter(htmlFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.MHTML))
            {
                Gtk.FileFilter mhtmlFilter = new Gtk.FileFilter { Name = "MHTML" };
                var extensionMHTML = ".mhtml";
                mhtmlFilter.AddPattern($"*{extensionMHTML}");
                fc.AddFilter(mhtmlFilter);
            }

            if (!restrictedOutputPresentationTypes.Contains(OutputPresentationType.XML))
            {
                Gtk.FileFilter xmlFilter = new Gtk.FileFilter { Name = "XML" };
                var extensionXML = ".xml";
                xmlFilter.AddPattern($"*{extensionXML}");
                fc.AddFilter(xmlFilter);
            }

            if (!fc.Filters.Any())
            {
                using var m = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Info,
                    Gtk.ButtonsType.Ok, false,
                    "Export in all document formats is prohibited");

                m.WindowPosition = WindowPosition.Center;
                m.Run();
                m.Destroy();
                return;
            }

            if (fc.Run() == (int)Gtk.ResponseType.Accept)
            {
                try
                {
                    string searchPattern = "*";

                    string filename = fc.Filename;
                    OutputPresentationType exportType = OutputPresentationType.PDF;
                    if (fc.Filter.Name == "CSV")
                    {
                        exportType = OutputPresentationType.CSV;
                        if (filename.ToLower().Trim().EndsWith(".csv") == false)
                        {
                            filename = filename + ".csv";
                            searchPattern = "*.csv";
                        }
                    }
                    else if (fc.Filter.Name == "PDF")
                    {
                        exportType = OutputPresentationType.PDF;
                        if (filename.ToLower().Trim().EndsWith(".pdf") == false)
                        {
                            filename = filename + ".pdf";
                            searchPattern = "*.pdf";
                        }
                    }
                    else if (fc.Filter.Name == "Excel 2007 Data")
                    {
                        exportType = OutputPresentationType.ExcelTableOnly;
                        if (filename.ToLower().Trim().EndsWith(".xlsx") == false)
                        {
                            filename = filename + ".xlsx";
                            searchPattern = "*.xlsx";
                        }
                    }
                    else if (fc.Filter.Name == "Excel 2007")
                    {
                        exportType = OutputPresentationType.Excel2007;
                        if (filename.ToLower().Trim().EndsWith(".xlsx") == false)
                        {
                            filename = filename + ".xlsx";
                            searchPattern = "*.xlsx";
                        }
                    }
                    else if (fc.Filter.Name == "TIFF")
                    {
                        exportType = OutputPresentationType.TIF;
                        if (filename.ToLower().Trim().EndsWith(".tif") == false)
                        {
                            filename = filename + ".tif";
                            searchPattern = "*.tif";
                        }
                    }
                    else if (fc.Filter.Name == "ASP HTML")
                    {
                        exportType = OutputPresentationType.ASPHTML;
                        if (filename.ToLower().Trim().EndsWith(".asphtml") == false)
                        {
                            filename = filename + ".asphtml";
                            searchPattern = "*.asphtml";
                        }
                    }
                    else if (fc.Filter.Name == "HTML")
                    {
                        exportType = OutputPresentationType.HTML;
                        if (filename.ToLower().Trim().EndsWith(".html") == false)
                        {
                            filename = filename + ".html";
                            searchPattern = "*.html";
                        }
                    }
                    else if (fc.Filter.Name == "MHTML")
                    {
                        exportType = OutputPresentationType.MHTML;
                        if (filename.ToLower().Trim().EndsWith(".mhtml") == false)
                        {
                            filename = filename + ".mhtml";
                            searchPattern = "*.mhtml";
                        }
                    }
                    else if (fc.Filter.Name == "XML")
                    {
                        exportType = OutputPresentationType.XML;
                        if (filename.ToLower().Trim().EndsWith(".xml") == false)
                        {
                            filename = filename + ".xml";
                            searchPattern = "*.xml";
                        }
                    }

                    string directory = System.IO.Path.GetDirectoryName(filename);

                    var files = Directory.GetFiles(directory, searchPattern);

                    //Check for files with same name in directory
                    if (files.Any())
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (files[i] == filename)
                            {
                                //If found files with the same name in directory
                                using var m = new Gtk.MessageDialog(null, DialogFlags.Modal, MessageType.Question,
                                    Gtk.ButtonsType.YesNo, false,
                                 Strings.SaveToFile_CheckIf_SameFilesInDir);

                                m.SetPosition(WindowPosition.Center);
                                ResponseType result = (ResponseType)m.Run();
                                m.Destroy();
                                if (result == ResponseType.Yes)
                                {
                                    // Must use the RunGetData before each export or there is no data.
                                    await report.RunGetData(this.Parameters);
                                    await ExportReport(report, filename, exportType);
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (i + 1 == files.Length && files[i] != filename)
                            {
                                //If no files with the same name found in directory
                                // Must use the RunGetData before each export or there is no data.
                                await report.RunGetData(this.Parameters);
                                await ExportReport(report, filename, exportType);
                                break;
                            }
                        }
                    }
                    else
                    {
                        //If no files found in directory
                        // Must use the RunGetData before each export or there is no data.
                        await report.RunGetData(this.Parameters);
                        await ExportReport(report, filename, exportType);
                    }
                }
                catch (Exception ex)
                {
                    using var m = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Info,
                               Gtk.ButtonsType.Ok, false,
                               $"Error Saving Copy of {fc.Filter?.Name}." + System.Environment.NewLine + ex.Message);

                    m.Run();
                    m.Destroy();
                }
            }
            //Don't forget to call Destroy() or the FileChooserDialog window won't get closed.
            fc.Destroy();

        }

        /// <summary>
        /// Save the report to the output selected.
        /// </summary>
        /// <param name='report'>
        /// Report.
        /// </param>
        /// <param name='FileName'>
        /// File name.
        /// </param>
        private async Task ExportReport(Report report, string FileName, OutputPresentationType exportType)
        {
            try
            {
                using var sg = new OneFileStreamGen(FileName, true);
                await report.RunRender(sg, exportType);
            }
            catch (Exception ex)
            {
                using var m = new Gtk.MessageDialog(null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error,
                              Gtk.ButtonsType.Ok, false,
                              ex.Message);

                m.Run();
                m.Destroy();
            }
            return;
        }

        protected void OnPrintActionActivated(object sender, System.EventArgs e)
        {
        }

        public void PrintReport()
        {
            using PrintContext context = new PrintContext(Window.Handle);
            if (customPrintAction == null)
            {
                printing = new PrintOperation();
                printing.Unit = Unit.Points;
                printing.UseFullPage = true;
                printing.DefaultPageSetup = new PageSetup();
                printing.DefaultPageSetup.Orientation =
                    report.PageHeightPoints > report.PageWidthPoints ? PageOrientation.Portrait : PageOrientation.Landscape;

                printing.BeginPrint += HandlePrintBeginPrint;
                printing.DrawPage += HandlePrintDrawPage;
                printing.EndPrint += HandlePrintEndPrint;

                printing.Run(PrintOperationAction.PrintDialog, null);
            }
            else
            {
                customPrintAction.Invoke(pages);
            }
        }

        void HandlePrintBeginPrint(object o, BeginPrintArgs args)
        {
            printing.NPages = pages.Count;
        }

        void HandlePrintDrawPage(object o, DrawPageArgs args)
        {
            if (args?.Context == null || pages == null)
            {
                return;
            }

            using (var g = args.Context.CairoContext)
            using (var render = new RenderCairo(g))
            {
                render.RunPage(pages[args.PageNr]);
            }
        }

        void HandlePrintEndPrint(object o, EndPrintArgs args)
        {
            ReportPrinted?.Invoke(this, EventArgs.Empty);
            printing.BeginPrint -= HandlePrintBeginPrint;
            printing.DrawPage -= HandlePrintDrawPage;
            printing.EndPrint -= HandlePrintEndPrint;
            printing.DefaultPageSetup.Dispose();
            printing.Dispose();
        }

        protected async void OnRefreshActionActivated(object sender, System.EventArgs e)
        {
            await RefreshReport();
        }

        int hpanedWidth = 0;
        
        protected void OnHpanedReportSizeAllocated(object o, Gtk.SizeAllocatedArgs args)
        {
            if (args.Allocation.Width != hpanedWidth)
            {
                hpanedWidth = args.Allocation.Width;
                // int textviewWidth = scrolledwindowErrors.Allocation.Width + 10;
                // hpanedReport.Position = hpanedWidth - textviewWidth;
                hpanedReport.Position = (int)(hpanedWidth * 0.8);
            }
        }


        public override void Destroy()
        {
            base.Destroy();
        }

        protected void OnDeleteEvent(object sender, DeleteEventArgs a)
        {
            errorsAction.Toggled -= OnErrorsActionToggled;
            pages?.Dispose();
            pages = null;
            report?.Dispose();
            Gtk.Application.Quit();
            a.RetVal = true;
        }
    }
}

