using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Configuration;
using System.Linq;
using DocsPAWA.DocsPaWR;

namespace DocsPAWA.trasmissione
{
    /// <summary>
    /// Summary description for trasmDatiTrasm_dx.
    /// </summary>
    public class trasmDatiTrasm_dx : DocsPAWA.CssPage
    {
        protected System.Web.UI.WebControls.Table tbl_Lista;
        protected DocsPAWA.DocsPaWR.Trasmissione trasmissione;
        protected int index;
        protected System.Web.UI.WebControls.Button Button1;
        protected System.Web.UI.HtmlControls.HtmlInputHidden flag_save;
        protected System.Web.UI.HtmlControls.HtmlInputHidden azione;
        protected System.Web.UI.WebControls.Label titolo;
        protected ArrayList arrayIndTab;
        protected int numeroRuoliDestInTrasmissione = 0;
        protected int numeroUtentiConNotifica = 0;
        protected string idPeopleNewOwner = string.Empty;
        protected DocsPAWA.DocsPaWR.ModelloTrasmissione modello;

        private void Page_Load(object sender, System.EventArgs e)
        {
            try
            {
                Utils.startUp(this);

                if (!IsPostBack)
                {
                    if (Request.QueryString["azione"] != null && !Request.QueryString["azione"].Equals(""))
                        this.azione.Value = Request.QueryString["azione"];
                    else
                        this.azione.Value = "Nuova";

                    if (this.azione.Value.Equals("Modifica"))
                        this.titolo.Text = "Modifica trasmissione";
                    else
                        this.titolo.Text = "Nuova trasmissione";

                    //gestione flag notifiche
                    this.releaseMemoriaNotificaUt();
                }

                trasmissione = TrasmManager.getGestioneTrasmissione(this);

                //modello = (DocsPAWA.DocsPaWR.ModelloTrasmissione)Session["Modello"];

                this.FillTableTrasmissioni();

                //gestione flag notifiche
                this.gestioneMemoriaNotificaUt();

            }
            catch (Exception ex)
            {
                ErrorManager.redirect(this, ex);
            }
        }

        private void FillTableTrasmissioni()
        {
            this.arrayIndTab = new ArrayList();
            if (trasmissione != null)
            {
                if (trasmissione.trasmissioniSingole != null)
                {
                    drawBorderRow();
                    drawHeader();

                    for (int i = 0; i < trasmissione.trasmissioniSingole.Length; i++)
                    {
                        drawTable(trasmissione.trasmissioniSingole[i].corrispondenteInterno, i);
                    }

                    drawBorderRow();
                }
            }
        }

        /// <summary>
        /// Verifica se una trasmissione singola possa essere cancellata
        /// (in presenza di un systemid)
        /// </summary>
        /// <param name="trasmissioneSingola"></param>
        /// <returns></returns>
        private bool IsTrasmissioneCancellabile(DocsPAWA.DocsPaWR.TrasmissioneSingola trasmissioneSingola)
        {
            if (this.azione.Value.Equals("Modifica"))  // sono in "Modifica"... sempre!
                return true;
            else
                return (trasmissioneSingola.systemId == null); // sono in "Nuova"... controllo inutile...!
        }

        private void trasmDatiTrasm_dx_Unload(object sender, System.EventArgs e)
        {
            foreach (TableRow row in this.tbl_Lista.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    foreach (Control ctr in cell.Controls)
                    {
                        CheckBox chk = ctr as CheckBox;
                        if (chk != null)
                        {
                            if (chk.Checked && chk.ID.StartsWith("chkNotifica_"))
                            {
                                chk.CheckedChanged -= new EventHandler(this.chkNotifica_CheckedChanged);
                            }
                        }
                    }

                }
            }
        }

        private void trasmDatiTrasm_dx_PreRender(object sender, System.EventArgs e)
        {
            try
            {
                if (this.flag_save.Value.Equals("E"))
                {
                    this.eliminaTrasmSalvata();
                }

                if (this.flag_save.Value.Equals("S") || this.flag_save.Value.Equals("ST") || this.flag_save.Value.Equals("STempl"))
                {
                    trasmissione = TrasmManager.getGestioneTrasmissione(this);

                    if (trasmissione != null && trasmissione.trasmissioniSingole == null || trasmissione.trasmissioniSingole.Length <= 0)
                    {
                        string msg = "Destinatari non inseriti!";
                        Response.Write("<script>alert('" + msg + "')</script>");
                        this.flag_save.Value = "N";
                        return;
                    }

                    ArrayList esiti = verificaSelezioneTrasmissioneUtente(trasmissione);

                    if (esiti != null && esiti.Count > 0)
                    {
                        // c� almeno una trasmissione singola e l'utente nn ha selezionato almeno una trasm utente in essa
                        //viene lanciato un avviso..ovvero bisogna selezionare almeno un utente in un ruolo!!! 
                        string message = "Attenzione - selezionare almeno un utente per i seguenti ruoli:\\n";

                        foreach (string str in esiti)
                        {
                            message = message + "\\n - " + str;
                        }
                        ClientScript.RegisterStartupScript(typeof(Page), "avviso", "<SCRIPT>alert(\"" + message + "\");</SCRIPT>");
                        this.flag_save.Value = "N";
                    }
                    else
                    {
                        this.salvaTrasm(this.flag_save.Value);
                        this.flag_save.Value = "N";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.redirect(this, ex);
            }
        }

        private ArrayList verificaSelezioneTrasmissioneUtente(DocsPaWR.Trasmissione trasmissione)
        {
            ArrayList esiti = new ArrayList();

            foreach (DocsPaWR.TrasmissioneSingola trasmSing in trasmissione.trasmissioniSingole)
            {
                bool esitoNotifica = false;
                if (trasmSing.trasmissioneUtente != null && trasmSing.trasmissioneUtente.Length > 0)
                {
                    //se ci sono pi� trasmissioni utente devo verificare che almeno una sia daNotificare
                    foreach (DocsPaWR.TrasmissioneUtente trasmUt in trasmSing.trasmissioneUtente)
                    {
                        if (trasmUt.daNotificare)
                        {
                            esitoNotifica = true;
                            break;
                        }
                    }
                    if (!esitoNotifica)
                    {
                        esiti.Add(trasmSing.corrispondenteInterno.descrizione);
                    }
                }
            }
            return esiti;
        }

        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            base.OnInit(e);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Load += new System.EventHandler(this.Page_Load);
            this.PreRender += new System.EventHandler(this.trasmDatiTrasm_dx_PreRender);
            this.Unload += new EventHandler(trasmDatiTrasm_dx_Unload);
        }
        #endregion

        #region Tabella
        private void drawBorderRow()
        {
            //riga separatrice(color rosa)
            TableRow tr = new TableRow();
            TableCell tc = new TableCell();
            tr.CssClass = "bg_grigioS";
            tc.ColumnSpan = 6;

            tc.Width = Unit.Percentage(100);
            tc.Height = Unit.Pixel(10);
            tr.Cells.Add(tc);
            //Aggiungo Row alla table1
            this.tbl_Lista.Rows.Add(tr);
        }

        private void drawHeader()
        {
            TableRow tr;
            TableCell tc;

            //riga titoli

            tr = new TableRow();
            //tr.ID = "tbl_corr_" + tbl_Lista.Rows.Count.ToString();
            tr.Height = Unit.Pixel(15);
            tr.CssClass = "menu_1_bianco";
            tr.BackColor = Color.FromArgb(149, 149, 149);
            tr.BorderColor = Color.DarkGray;
            //destinatatio
            tc = new TableCell();
            tc.Width = Unit.Percentage(33);
            tc.Text = "Destinatario";
            tr.Cells.Add(tc);
            //ragione
            tc = new TableCell();
            tc.Width = Unit.Percentage(30);
            tc.Text = "Ragione";
            tr.Cells.Add(tc);
            //tipo
            tc = new TableCell();
            tc.Width = Unit.Percentage(20);
            tc.Text = "Tipo";
            tr.Cells.Add(tc);
            //note
            tc = new TableCell();
            //tc.Width = Unit.Percentage(30);
            tc.Text = "Note";
            tr.Cells.Add(tc);
            //data scad
            tc = new TableCell();
            tc.Width = Unit.Percentage(20);
            tc.Text = "Data scad";
            tr.Cells.Add(tc);

            //cancella
            tc = new TableCell();

            DocsPaWebCtrlLibrary.ImageButton btn_canc = new DocsPaWebCtrlLibrary.ImageButton();
            btn_canc.ImageUrl = "../images/proto/cancella.gif";
            btn_canc.ID = "btn_canc";
            btn_canc.Click += new System.Web.UI.ImageClickEventHandler(this.FunzCanc);
            btn_canc.AlternateText = "Cancella";
            tc.Controls.Add(btn_canc);

            tr.Cells.Add(tc);

            //Aggiungo Row alla table1
            this.tbl_Lista.Rows.Add(tr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idCorrispondente"></param>
        /// <returns></returns>
        private bool drawHideDocumentVersions(int idCorrispondente)
        {
            bool retValue = false;

            DocsPaWR.SchedaDocumento documentoCorrente = DocumentManager.getDocumentoSelezionato();

            if (documentoCorrente != null &&
                documentoCorrente.ConsolidationState != null &&
                documentoCorrente.ConsolidationState.State > DocsPaWR.DocumentConsolidationStateEnum.None)
            {
                TableRow tr = new TableRow();
                tr.Height = Unit.Pixel(15);
                tr.BackColor = Color.FromArgb(242, 242, 242);
                tr.BorderColor = Color.FromArgb(217, 217, 217);

                TableCell tcuVuota = new TableCell();
                tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                tcuVuota.CssClass = "testo_grigio";
                tcuVuota.ColumnSpan = 1;
                tr.Cells.Add(tcuVuota);

                TableCell tc = new TableCell();
                tc.HorizontalAlign = HorizontalAlign.Left;
                tc.ColumnSpan = 2;
                this.drawHideDocumentVersions(tc, idCorrispondente);
                tr.Cells.Add(tc);

                for (int i = 1; i < 5; i++)
                {
                    tcuVuota = new TableCell();
                    tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                    tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                    tcuVuota.CssClass = "testo_grigio";
                    tcuVuota.ColumnSpan = 1;
                    tr.Cells.Add(tcuVuota);
                }

                //Aggiungo Row alla table1
                this.tbl_Lista.Rows.Add(tr);

                retValue = true;
            }

            return retValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="idCorrispondente"></param>
        /// <returns></returns>
        private bool drawHideDocumentVersions(TableCell tc, int idCorrispondente)
        {
            bool retValue = false;

            DocsPaWR.SchedaDocumento documentoCorrente = DocumentManager.getDocumentoSelezionato();

            if (documentoCorrente != null &&
                documentoCorrente.ConsolidationState != null &&
                documentoCorrente.ConsolidationState.State > DocsPaWR.DocumentConsolidationStateEnum.None)
            {
                // Flag nascondi versioni precedenti
                CheckBox chkHideDocumentPreviousVersions = new CheckBox();
                chkHideDocumentPreviousVersions.ID = "chkHideDocumentPreviousVersions_" + idCorrispondente;
                chkHideDocumentPreviousVersions.Text = "Nascondi versioni precedenti";
                chkHideDocumentPreviousVersions.CssClass = "testo_grigio";
                chkHideDocumentPreviousVersions.TextAlign = TextAlign.Right;
                chkHideDocumentPreviousVersions.AutoPostBack = true;

                tc.ColumnSpan = 3;

                if (documentoCorrente.previousVersionsHidden)
                {
                    // Per l'utente corrente risultano nascoste le versioni prececenti a quella corrente,
                    // pertanto il flag nascondi versioni � disabilitato e impostato per default
                    chkHideDocumentPreviousVersions.Checked = true;
                    chkHideDocumentPreviousVersions.Enabled = false;
                    trasmissione.trasmissioniSingole[idCorrispondente].hideDocumentPreviousVersions = true;
                }
                else
                {
                    chkHideDocumentPreviousVersions.CheckedChanged += new EventHandler(this.chkHideDocumentPreviousVersions_CheckedChanged);
                    chkHideDocumentPreviousVersions.Checked = this.trasmissione.trasmissioniSingole[idCorrispondente].hideDocumentPreviousVersions;
                    chkHideDocumentPreviousVersions.Enabled = true;
                }

                tc.Controls.Add(chkHideDocumentPreviousVersions);

                retValue = true;
            }

            return retValue;
        }

        private void drawSelectAll(string idCorrispondente, int numUtenti)
        {
            TableRow tr;
            TableCell tc;

            //riga titoli

            tr = new TableRow();
            //tr.ID = "tbl_corr_" + tbl_Lista.Rows.Count.ToString();
            tr.Height = Unit.Pixel(15);
            //tr.CssClass = "menu_grigio";
            tr.BackColor = Color.FromArgb(242, 242, 242);
            tr.BorderColor = Color.FromArgb(217, 217, 217);

            //seleziona tutti


            tc = new TableCell();
            CheckBox chkSelAll = new CheckBox();
            chkSelAll.ID = "chkSelAll_" + idCorrispondente + "_" + numUtenti.ToString();// +"_" + j.ToString();            
            chkSelAll.Text = "Seleziona tutti";
            chkSelAll.CssClass = "testo_grigio";
            chkSelAll.TextAlign = TextAlign.Right;
            chkSelAll.AutoPostBack = true;
            chkSelAll.CheckedChanged += new EventHandler(this.chkSelAll_CheckedChanged);
            chkSelAll.Checked = TrasmManager.getTxRuoloUtentiChecked();
            tc.HorizontalAlign = HorizontalAlign.Left;
            tc.Controls.Add(chkSelAll);
            // tc.ColumnSpan = 1;
            // tr.Cells.Add(tc);

            //this.drawHideDocumentVersions(tc, Convert.ToInt32(idCorrispondente));

            int numeroCorr = Convert.ToInt32(idCorrispondente);
            TableCell tcNotifica = new TableCell();
            tcNotifica.BorderColor = Color.FromArgb(217, 217, 217);
            tcNotifica.BackColor = Color.FromArgb(242, 242, 242);
            tcNotifica.CssClass = "testo_grigio";
            tcNotifica.ColumnSpan = 3;

            TableCell tcuVuota = new TableCell();

            DocsPaWR.SchedaDocumento documentoCorrente = DocumentManager.getDocumentoSelezionato();

            if (documentoCorrente != null &&
                documentoCorrente.ConsolidationState != null &&
                documentoCorrente.ConsolidationState.State > DocsPaWR.DocumentConsolidationStateEnum.None)
            {
                // Flag nascondi versioni precedenti
                CheckBox chkHideDocumentPreviousVersions = new CheckBox();
                chkHideDocumentPreviousVersions.ID = "chkHideDocumentPreviousVersions_" + idCorrispondente;
                chkHideDocumentPreviousVersions.Text = "Nascondi versioni precedenti";
                chkHideDocumentPreviousVersions.CssClass = "testo_grigio";
                chkHideDocumentPreviousVersions.TextAlign = TextAlign.Right;
                chkHideDocumentPreviousVersions.AutoPostBack = true;

                tc.ColumnSpan = 1;
                tr.Cells.Add(tc);

                if (documentoCorrente.previousVersionsHidden)
                {
                    // Per l'utente corrente risultano nascoste le versioni prececenti a quella corrente,
                    // pertanto il flag nascondi versioni � disabilitato e impostato per default
                    chkHideDocumentPreviousVersions.Checked = true;
                    chkHideDocumentPreviousVersions.Enabled = false;
                    trasmissione.trasmissioniSingole[numeroCorr].hideDocumentPreviousVersions = true;
                }
                else
                {
                    chkHideDocumentPreviousVersions.CheckedChanged += new EventHandler(this.chkHideDocumentPreviousVersions_CheckedChanged);
                    chkHideDocumentPreviousVersions.Checked = this.trasmissione.trasmissioniSingole[numeroCorr].hideDocumentPreviousVersions;
                    chkHideDocumentPreviousVersions.Enabled = true;
                }

                tcNotifica.Controls.Add(chkHideDocumentPreviousVersions);
                tr.Cells.Add(tcNotifica);
            }
            else
            {
                tc.ColumnSpan = 2;
                tr.Cells.Add(tc);

                tcuVuota = new TableCell();
                tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                tcuVuota.CssClass = "testo_grigio";
                tcuVuota.ColumnSpan = 1;
                tr.Cells.Add(tcuVuota);

                tcuVuota = new TableCell();
                tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                tcuVuota.CssClass = "testo_grigio";
                tcuVuota.ColumnSpan = 1;
                tr.Cells.Add(tcuVuota);


                tcuVuota = new TableCell();
                tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                tcuVuota.CssClass = "testo_grigio";
                tcuVuota.ColumnSpan = 1;
                tr.Cells.Add(tcuVuota);
            }

            tcuVuota = new TableCell();
            tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
            tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
            tcuVuota.CssClass = "testo_grigio";
            tcuVuota.ColumnSpan = 1;
            tr.Cells.Add(tcuVuota);

            tcuVuota = new TableCell();
            tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
            tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
            tcuVuota.CssClass = "testo_grigio";
            tcuVuota.ColumnSpan = 1;
            tr.Cells.Add(tcuVuota);

            //Aggiungo Row alla table1
            this.tbl_Lista.Rows.Add(tr);
        }


        protected void drawTable(DocsPAWA.DocsPaWR.Corrispondente Corr, int idCorrispondente)
        {
            try
            {
                int numRiga = tbl_Lista.Rows.Count;
                index = numRiga;

                TableRow tr = new TableRow();
                tr.ID = "tbl_corr_" + numRiga.ToString();
                tr.CssClass = "testo_grigio";
                tr.BackColor = Color.FromArgb(242, 242, 242);
                tr.Height = Unit.Pixel(20);

                //cell:0 destinatario
                TableCell tc = new TableCell();
                tc.Text = Corr.descrizione;
                tr.Cells.Add(tc);
                //cell:1 ragione
                tc = new TableCell();
                tc.Text = trasmissione.trasmissioniSingole[idCorrispondente].ragione.descrizione;
                tr.Cells.Add(tc);
                //cell:2 tipo  ??? controllo
                tc = new TableCell();
                //tc.Height=Unit.Pixel(50);

                if (Corr.GetType() == typeof(DocsPAWA.DocsPaWR.Ruolo))
                {
                    //commentato per introduzione del concetto UNO/TUTTI anche per regioni
                    //di trasmissione che non prevedono il workflow
                    //if (trasmissione.trasmissioniSingole[idCorrispondente].ragione.tipo.Equals("W"))
                    //{
                    DropDownList ddl_tipo = new DropDownList();
                    ddl_tipo.ID = "ddl_tipo_" + idCorrispondente;
                    ddl_tipo.AutoPostBack = false;
                    ddl_tipo.CssClass = "testo_grigio";
                    ddl_tipo.Enabled = true;
                    ddl_tipo.Items.Add("Tutti");
                    //ddl_tipo.Height=Unit.Pixel(200);
                    ddl_tipo.Items[0].Value = "T";
                    ddl_tipo.Items.Add("Uno");
                    ddl_tipo.Items[1].Value = "S";

                    ddl_tipo.SelectedIndexChanged += new EventHandler(this.ddl_tipo_SelectedIndexChanged);

                    //diventa true se ragione � di un certo tipo
                    if (trasmissione.trasmissioniSingole[idCorrispondente].tipoTrasm == "T")
                        ddl_tipo.Items[0].Selected = true;
                    else
                        ddl_tipo.Items[1].Selected = true;
                    tc.Controls.Add(ddl_tipo);
                    //}
                }
                tr.Cells.Add(tc);
                //cell:3 note individuali
                tc = new TableCell();
                TextBox txt_NoteInd = new TextBox();
                txt_NoteInd.ID = "txt_NoteInd_" + idCorrispondente;
                txt_NoteInd.Width = 150;
                txt_NoteInd.MaxLength = 250;
                txt_NoteInd.CssClass = "testo_grigio";
                txt_NoteInd.Text = trasmissione.trasmissioniSingole[idCorrispondente].noteSingole;
                txt_NoteInd.AutoPostBack = false;
                txt_NoteInd.TextChanged += new EventHandler(this.txt_NoteInd_TextChanged);
                tc.Controls.Add(txt_NoteInd);
                tr.Cells.Add(tc);
                //cell:4 data scad
                tc = new TableCell();
                tc.CssClass = "testo_grigio";

                DocsPaWebCtrlLibrary.DateMask txt_DataScad = new DocsPaWebCtrlLibrary.DateMask();
                txt_DataScad.ID = "txt_DataScad_" + idCorrispondente;
                txt_DataScad.CssClass = "testo_grigio";
                txt_DataScad.MaxLength = 10;
                txt_DataScad.Text = trasmissione.trasmissioniSingole[idCorrispondente].dataScadenza;

                if (!trasmissione.trasmissioniSingole[idCorrispondente].ragione.tipo.Equals("W"))
                {
                    txt_DataScad.ReadOnly = true;
                    txt_DataScad.BackColor = Color.FromArgb(217, 217, 217);
                }
                else
                    txt_DataScad.BackColor = Color.FromArgb(255, 255, 255);
                txt_DataScad.AutoPostBack = false;
                txt_DataScad.TextChanged += new EventHandler(this.txt_DataScad_TextChanged);
                tc.Controls.Add(txt_DataScad);
                tr.Cells.Add(tc);
                //cell:5 cancella tasto
                tc = new TableCell();
                tc.Width = Unit.Percentage(5);
                tc.Height = Unit.Pixel(20);

                CheckBox chkCanc = new CheckBox();
                chkCanc.ID = "chkCanc_" + idCorrispondente;
                chkCanc.Enabled = this.IsTrasmissioneCancellabile(trasmissione.trasmissioniSingole[idCorrispondente]);
                tc.Controls.Add(chkCanc);

                tr.Cells.Add(tc);

                //Aggiungo Row alla table1
                this.tbl_Lista.Rows.Add(tr);

                //inserisco indice nell'arrayList che servir� per scorrere le righe dove sono presenti i campi
                arrayIndTab.Add(index);
                index++;

                if (Corr.GetType() == typeof(DocsPAWA.DocsPaWR.Ruolo))
                {
                    drawSelectAll(idCorrispondente.ToString(),
                            trasmissione.trasmissioniSingole[idCorrispondente].trasmissioneUtente.Length);
                    index++;
                }
                else
                {
                    if (this.drawHideDocumentVersions(idCorrispondente))
                        index++;
                }

                //ciclo per utenti
                DocsPaWR.TrasmissioneUtente[] trasmissioniUtente = trasmissione.trasmissioniSingole[idCorrispondente].trasmissioneUtente;
                for (int j = 0; j < trasmissioniUtente.Length; j++)
                {
                    DocsPaWR.Utente utente = trasmissioniUtente[j].utente;

                    DocsPAWA.DocsPaWR.DocsPaWebService ws = new DocsPAWA.DocsPaWR.DocsPaWebService();
                    if (!ws.isUserDisabled(utente.codiceRubrica, utente.idAmministrazione))
                    {

                        TableRow tru = new TableRow();
                        // TableCell tcu = new TableCell();

                        tru.ID = "tbl_ut_" + numRiga.ToString() + "_" + j.ToString();

                        /*  tcu.BackColor = Color.FromArgb(242, 242, 242);
                        tcu.CssClass = "testo_grigio";
                        tcu.ColumnSpan = 4;
                        tcu.HorizontalAlign = HorizontalAlign.Left;
                        tcu.BorderColor = Color.FromArgb(217, 217, 217);
                        tcu.Text = "&nbsp;&nbsp;" + utente.descrizione;/*

                        tru.Cells.Add(tcu);*/

                        TableCell tcuN = new TableCell();
                        CheckBox chkNotifica = new CheckBox();
                        chkNotifica.ID = "chkNotifica_" + idCorrispondente + "_" + numRiga.ToString() + "_" + j.ToString();
                        chkNotifica.Text = "&nbsp;&nbsp;" + utente.descrizione;
                        chkNotifica.TextAlign = TextAlign.Right;
                        chkNotifica.AutoPostBack = true;

                        chkNotifica.CheckedChanged += new EventHandler(this.chkNotifica_CheckedChanged);

                        if (trasmissioniUtente.Length == 1)
                        {
                            //se l'utente � solo uno, di default disabilito il controllo e lo seleziono.
                            //Si deve trasmettere ad almeno ad un utente del ruolo.
                            chkNotifica.Enabled = false;
                            chkNotifica.Checked = true;
                            trasmissioniUtente[j].daNotificare = true;
                        }
                        else
                        {
                            bool valNotifica;

                            //if (modello != null)
                            //    valNotifica = this.selezionaNotificaDaModello(Corr, utente, modello);                        
                            //else
                            valNotifica = this.selezionaNotifica(trasmissioniUtente[j], idCorrispondente);

                            chkNotifica.Checked = valNotifica;
                            trasmissioniUtente[j].daNotificare = valNotifica;
                        }

                        tcuN.HorizontalAlign = HorizontalAlign.Left;
                        tcuN.BorderColor = Color.FromArgb(217, 217, 217);
                        tcuN.BackColor = Color.FromArgb(242, 242, 242);
                        tcuN.CssClass = "testo_grigio";
                        tcuN.ColumnSpan = 2;
                        tcuN.Controls.Add(chkNotifica);

                        tru.Cells.Add(tcuN);

                        TableCell tcuVuota = new TableCell();
                        tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                        tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                        tcuVuota.CssClass = "testo_grigio";
                        tcuVuota.ColumnSpan = 1;
                        tru.Cells.Add(tcuVuota);



                        tcuVuota = new TableCell();
                        tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                        tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                        tcuVuota.CssClass = "testo_grigio";
                        tcuVuota.ColumnSpan = 1;
                        tru.Cells.Add(tcuVuota);



                        tcuVuota = new TableCell();
                        tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                        tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                        tcuVuota.CssClass = "testo_grigio";
                        tcuVuota.ColumnSpan = 1;
                        tru.Cells.Add(tcuVuota);



                        tcuVuota = new TableCell();
                        tcuVuota.BorderColor = Color.FromArgb(217, 217, 217);
                        tcuVuota.BackColor = Color.FromArgb(242, 242, 242);
                        tcuVuota.CssClass = "testo_grigio";
                        tcuVuota.ColumnSpan = 1;
                        tru.Cells.Add(tcuVuota);

                        this.tbl_Lista.Rows.Add(tru);

                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.redirect(this, ex);
            }
        }

        private void chkNotifica_CheckedChanged(object sender, System.EventArgs e)
        {
            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);
            string IdCheckNotifica = ((CheckBox)sender).ID;
            char[] IdSeparator = { '_' };
            int rowSingola = Int32.Parse((IdCheckNotifica.Split(IdSeparator)[1]));
            int rowUtente = Int32.Parse((IdCheckNotifica.Split(IdSeparator)[3]));

            trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[rowUtente].daNotificare = ((CheckBox)(sender)).Checked;

            TrasmManager.setGestioneTrasmissione(this, trasmissione);

            this.modifyHashNotificaUt(rowSingola, trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[rowUtente].utente.idPeople, ((CheckBox)(sender)).Checked);
            if (!((CheckBox)(sender)).Checked)
                if (trasmissione.trasmissioniSingole[rowSingola].corrispondenteInterno.GetType() == typeof(DocsPAWA.DocsPaWR.Ruolo))
                {
                    ((CheckBox)this.tbl_Lista.Rows[(int)arrayIndTab[rowSingola] + 1].Cells[0].Controls[0]).Checked = ((CheckBox)(sender)).Checked;
                }
        }

        protected void chkHideDocumentPreviousVersions_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkHidePreviousVersions = (CheckBox)sender;
            string idCheckNascondiVersioni = chkHidePreviousVersions.ID;

            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);

            int rowTxSingola = Int32.Parse(idCheckNascondiVersioni.Split(new char[1] { '_' })[1]);

            trasmissione.trasmissioniSingole[rowTxSingola].hideDocumentPreviousVersions = chkHidePreviousVersions.Checked;

            TrasmManager.setGestioneTrasmissione(this, trasmissione);
        }

        private void chkSelAll_CheckedChanged(object sender, System.EventArgs e)
        {
            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);
            string IdCheckSelAll = ((CheckBox)sender).ID;
            char[] IdSeparator = { '_' };
            int rowSingola = Int32.Parse((IdCheckSelAll.Split(IdSeparator)[1]));
            int numUtenti = Int32.Parse((IdCheckSelAll.Split(IdSeparator)[2]));
            for (int i = 0; i < numUtenti; i++)
            {
                trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[i].daNotificare = ((CheckBox)(sender)).Checked;
                TrasmManager.setGestioneTrasmissione(this, trasmissione);
                this.modifyHashNotificaUt(rowSingola, trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[i].utente.idPeople, ((CheckBox)(sender)).Checked);
                //((CheckBox)this.tbl_Lista.Rows[(int)arrayIndTab[rowSingola] + 2 + i].Cells[1].Controls[0]).Checked = ((CheckBox)(sender)).Checked;
                ((CheckBox)this.tbl_Lista.Rows[(int)arrayIndTab[rowSingola] + 2 + i].Cells[0].Controls[0]).Checked = ((CheckBox)(sender)).Checked;
            }


            //FillTableTrasmissioni();
            //trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[rowUtente].daNotificare = ((CheckBox)(sender)).Checked;

            //TrasmManager.setGestioneTrasmissione(this, trasmissione);

            //this.modifyHashNotificaUt(rowSingola, trasmissione.trasmissioniSingole[rowSingola].trasmissioneUtente[rowUtente].utente.idPeople, ((CheckBox)(sender)).Checked);
        }


        /// <summary>
        /// Cancellazione delle trasmissioni selezionate in tabella
        /// </summary>
        private void RemoveTrasmissioniSingole()
        {
            ArrayList selectedID = new ArrayList();
            bool setDaEliminare = true;

            foreach (TableRow row in tbl_Lista.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    foreach (Control ctr in cell.Controls)
                    {
                        CheckBox chk = ctr as CheckBox;
                        if (chk != null)
                        {
                            if (chk.Checked && chk.ID.StartsWith("chkCanc_"))
                            {
                                selectedID.Add(chk.ID.Replace("chkCanc_", ""));
                            }
                        }
                    }
                }
            }

            if (selectedID.Count > 0)
            {
                for (int i = selectedID.Count - 1; i >= 0; i--)
                {
                    setDaEliminare = (trasmissione.trasmissioniSingole[i].systemId != null && !trasmissione.trasmissioniSingole[i].systemId.Equals(""));

                    if (this.azione.Value.Equals("Modifica"))  // sono in "Modifica"
                        setDaEliminare = false;

                    trasmissione = TrasmManager.removeTrasmSingola(trasmissione, Convert.ToInt32(selectedID[i]), setDaEliminare);

                    this.removeHashNotificaUt(Convert.ToInt32(selectedID[i]));
                }

                TrasmManager.setGestioneTrasmissione(this, trasmissione);

                this.tbl_Lista.Rows.Clear();
                this.FillTableTrasmissioni();
            }
            else
            {
                Response.Write("<script>alert('Nessuna trasmissione selezionata');</script>");
            }

            selectedID = null;
        }

        private void FunzCanc(object sender, System.Web.UI.ImageClickEventArgs e)
        {
            this.RemoveTrasmissioniSingole();
        }
        #endregion

        #region Trasmissioni
        private bool impostaDatiTrasmissione()
        {
            try
            {
                string dataScad;
                if (trasmissione == null)
                    trasmissione = TrasmManager.getGestioneTrasmissione(this);

                if (trasmissione != null)
                {
                    if (trasmissione.trasmissioniSingole == null || trasmissione.trasmissioniSingole.Length <= 0)
                    {
                        string msg = "Destinatari non inseriti!";
                        Response.Write("<script>alert('" + msg + "')</script>");
                        return false;
                    }

                    bool eredita = false;
                    //aggiorno le trasmissioni singole 
                    for (int h = 0; h < arrayIndTab.Count; h++)
                    {
                        if (trasmissione.trasmissioniSingole[h].corrispondenteInterno.GetType() == typeof(DocsPAWA.DocsPaWR.Ruolo))
                        {
                            //Cell Tipo
                            if (trasmissione.trasmissioniSingole[h].ragione.tipo.Equals("W"))
                            {
                                trasmissione.trasmissioniSingole[h].tipoTrasm = ((DropDownList)this.tbl_Lista.Rows[(int)arrayIndTab[h]].Cells[2].Controls[0]).SelectedItem.Value;
                            }
                        }
                        //Cell Note Ind
                        trasmissione.trasmissioniSingole[h].noteSingole = ((TextBox)this.tbl_Lista.Rows[(int)arrayIndTab[h]].Cells[3].Controls[0]).Text;
                        //Cell DataScad
                        //controllo sulla data
                        dataScad = ((TextBox)this.tbl_Lista.Rows[(int)arrayIndTab[h]].Cells[4].Controls[0]).Text;
                        if (dataScad != null && !dataScad.Equals(""))
                        {
                            if (!Utils.isDate(dataScad))
                            {
                                Response.Write("<script>alert('Il formato della Data Scadenza non � valido. \\nIl formato richiesto � gg/mm/aaaa');</script>");
                                return false;
                            }
                        }

                        trasmissione.trasmissioniSingole[h].dataScadenza = ((TextBox)this.tbl_Lista.Rows[(int)arrayIndTab[h]].Cells[4].Controls[0]).Text;
                        trasmissione.trasmissioniSingole[h].tipoDest = GetTipoUt(trasmissione.trasmissioniSingole[h].corrispondenteInterno);
                        if (trasmissione.trasmissioniSingole[h].ragione.eredita == "1")
                            eredita = true;
                    }

                    //aggiungo infoDocumento
                    DocsPaWR.SchedaDocumento schedaDocumento = DocumentManager.getDocumentoSelezionato(this);
                    trasmissione.infoDocumento = DocumentManager.getInfoDocumento(schedaDocumento);

                    return true;
                }

                return false;
            }
            catch (System.Web.Services.Protocols.SoapException es)
            {
                System.Web.Services.Protocols.SoapException et = es;
                Response.Redirect("ErrorPage.aspx?error=" + es.Message.ToString());
                return false;
            }
        }

        private void salvaTrasm(string tipo)
        {
            bool aperturaPopUpSceltaNuovoProprietario = false;

            try
            {
                DocsPaWR.SchedaDocumento sd = DocumentManager.getDocumentoSelezionato(this);

                //gestione flag notifiche
                this.impostaHashNotificheInObjTrasm();

                //Salvo il modello non la trasmissione e non effettuo il redirect
                if (tipo.Equals("STempl"))
                {
                    //Controllo la presenza di destinatari
                    if (trasmissione.trasmissioniSingole == null || trasmissione.trasmissioniSingole.Length <= 0)
                    {
                        string msg = "Destinatari non inseriti!";
                        Response.Write("<script>alert('" + msg + "')</script>");
                        return;
                    }

                    //SALVA MODELLO NUOVO
                    DocsPaWR.ModelloTrasmissione modello = null;
                    if (sd.registro == null)
                        modello = TrasmManager.getModelloTrasmNuovo(trasmissione, null);
                    else
                        modello = TrasmManager.getModelloTrasmNuovo(trasmissione, sd.registro.systemId);


                    // gestione cessione diritti
                    if (this.cessioneDirittiAbilitato())
                    {
                        // verifica la propriet� del documento da parte dell'utente
                        if (this.utenteProprietario(sd.docNumber))
                        {
                            // verifica se esistono ruoli tra i destinatari
                            this.verificaRuoliDestInTrasmissione();

                            switch (this.numeroRuoliDestInTrasmissione)
                            {
                                case 0:
                                    // non ci sono ruoli tra i destinatari! avvisa...
                                    this.inviaMsgNoRuoli();
                                    return;
                                    break;

                                case 1:
                                    // ce n'� 1, verifica se un solo utente del ruolo ha la notifica...
                                    this.utentiConNotifica();
                                    if (this.numeroUtentiConNotifica > 1)
                                        aperturaPopUpSceltaNuovoProprietario = true;
                                    else
                                    {
                                        // 1 solo utente con notifica, il sistema ha gi� memorizzato il suo id_people...
                                        trasmissione.cessione.idPeopleNewPropr = this.idPeopleNewOwner;
                                        trasmissione.cessione.idRuoloNewPropr = ((DocsPAWA.DocsPaWR.Ruolo)trasmissione.trasmissioniSingole[0].corrispondenteInterno).idGruppo;

                                        modello.CEDE_DIRITTI = "1";
                                        modello.ID_PEOPLE_NEW_OWNER = trasmissione.cessione.idPeopleNewPropr;
                                        modello.ID_GROUP_NEW_OWNER = trasmissione.cessione.idRuoloNewPropr;
                                    }
                                    break;

                                default:
                                    // ce ne sono + di 1, quindi ne fa scegliere uno...                                    
                                    aperturaPopUpSceltaNuovoProprietario = true;
                                    break;
                            }
                        }
                    }

                    Session.Add("Modello", modello);

                    if (this.impostaDatiTrasmissione())
                    {
                        TrasmManager.setGestioneTrasmissione(this, trasmissione);

                        if (aperturaPopUpSceltaNuovoProprietario)
                            this.aprePopUpSceltaNuovoProprietario(tipo);
                        else
                            this.aprePopUpSalvaModTrasm();
                    }
                    return;
                }

                // gestione cessione diritti
                if (this.cessioneDirittiAbilitato())
                {
                    // verifica la propriet� del documento da parte dell'utente
                    if (this.utenteProprietario(sd.docNumber))
                    {
                        // verifica se esistono ruoli tra i destinatari
                        this.verificaRuoliDestInTrasmissione();

                        switch (this.numeroRuoliDestInTrasmissione)
                        {
                            case 0:
                                // non ci sono ruoli tra i destinatari! avvisa...
                                this.inviaMsgNoRuoli();
                                return;
                                break;

                            case 1:
                                // ce n'� 1, verifica se un solo utente del ruolo ha la notifica...
                                this.utentiConNotifica();
                                if (this.numeroUtentiConNotifica > 1)
                                    aperturaPopUpSceltaNuovoProprietario = true;
                                else
                                {
                                    // 1 solo utente con notifica, il sistema ha gi� memorizzato il suo id_people...
                                    trasmissione.cessione.idPeopleNewPropr = this.idPeopleNewOwner;
                                    trasmissione.cessione.idRuoloNewPropr = ((DocsPAWA.DocsPaWR.Ruolo)trasmissione.trasmissioniSingole[0].corrispondenteInterno).idGruppo;
                                }
                                break;

                            default:
                                // ce ne sono + di 1, quindi ne fa scegliere uno...                                    
                                aperturaPopUpSceltaNuovoProprietario = true;
                                break;
                        }
                    }
                }

                if (this.impostaDatiTrasmissione())
                {
                    TrasmManager.setGestioneTrasmissione(this, trasmissione);

                    if (aperturaPopUpSceltaNuovoProprietario)
                    {
                        this.aprePopUpSceltaNuovoProprietario(tipo);
                        return;
                    }
                }
                else
                    return;

                //salva la trasmissione
                DocsPAWA.DocsPaWR.InfoUtente infoUtente = UserManager.getInfoUtente();
                if (infoUtente.delegato != null)
                    trasmissione.delegato = ((DocsPAWA.DocsPaWR.InfoUtente)(infoUtente.delegato)).idPeople;

                if (Session["Modello"] != null)
                {
                    DocsPAWA.DocsPaWR.ModelloTrasmissione mod = (DocsPAWA.DocsPaWR.ModelloTrasmissione)Session["Modello"];
                    if (mod != null)
                        trasmissione.NO_NOTIFY = mod.NO_NOTIFY;
                }

                if (tipo.Equals("S"))
                    trasmissione = TrasmManager.saveTrasm(this, trasmissione);
                else
                {
                    //Informazioni del delegato

                    //Nuova gestione salva e trasmetti
                    trasmissione = TrasmManager.saveExecuteTrasm(this, trasmissione, infoUtente);
                }

                // Tolto perch� non funzionante
                //if (trasmissione != null && !trasmissione.dirittiCeduti && this.cessioneDirittiAbilitato())
                //{
                //    //Response.Write("<script language='javascript'>window.alert('Non � stato possibile cedere i diritti con questo tipo di trasmissione.');</script>");
                //}
                trasmissione.daAggiornare = false;

                TrasmManager.removeGestioneTrasmissione(this);
                TrasmManager.setGestioneTrasmissione(this, trasmissione);
                TrasmManager.setDocTrasmSel(this, trasmissione);
                this.releaseMemoriaNotificaUt(); //gestione flag notifiche

                if (tipo.Equals("ST"))
                {
                    Session.Remove("doTrasm");
                    Session.Add("doTrasm", trasmissione);
                }

                if (!ClientScript.IsStartupScriptRegistered("redirect"))
                    ClientScript.RegisterStartupScript(this.GetType(), "redirect", "<script>redirect();</script>");

                this.RestoreCallerContext();

            }
            catch (System.Web.Services.Protocols.SoapException es)
            {
                System.Web.Services.Protocols.SoapException et = es;
                Response.Redirect("ErrorPage.aspx?error=" + es.Message.ToString());
            }
        }

        /// <summary>
        /// Apre una finestra per inserire il nome del modello e salvarlo
        /// </summary>
        private void aprePopUpSalvaModTrasm()
        {
            if (!ClientScript.IsStartupScriptRegistered("salvaModTrasm"))
                ClientScript.RegisterStartupScript(this.GetType(), "salvaModTrasm", "<script language=javascript>apriSalvaModTrasm();</script>");
        }

        protected DocsPAWA.DocsPaWR.TrasmissioneTipoDestinatario GetTipoUt(DocsPAWA.DocsPaWR.Corrispondente corr)
        {
            if (corr.GetType() == typeof(DocsPAWA.DocsPaWR.Ruolo))
                return DocsPAWA.DocsPaWR.TrasmissioneTipoDestinatario.RUOLO;
            else
                if (corr.GetType() == typeof(DocsPAWA.DocsPaWR.Utente))
                    return DocsPAWA.DocsPaWR.TrasmissioneTipoDestinatario.UTENTE;
            return DocsPAWA.DocsPaWR.TrasmissioneTipoDestinatario.GRUPPO;
        }

        private void txt_NoteInd_TextChanged(object sender, System.EventArgs e)
        {
            int idCorrispondente = Int32.Parse(((TextBox)sender).ID.Substring(12));
            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);
            trasmissione.trasmissioniSingole[idCorrispondente].noteSingole = ((TextBox)sender).Text;
            trasmissione.trasmissioniSingole[idCorrispondente].daAggiornare = true;
        }

        private void txt_DataScad_TextChanged(object sender, System.EventArgs e)
        {
            int idCorrispondente = Int32.Parse(((TextBox)sender).ID.Substring(13));
            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);
            trasmissione.trasmissioniSingole[idCorrispondente].dataScadenza = ((TextBox)sender).Text;
            trasmissione.trasmissioniSingole[idCorrispondente].daAggiornare = true;
        }

        private void ddl_tipo_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int idCorrispondente = Int32.Parse(((DropDownList)sender).ID.Substring(9));
            DocsPaWR.Trasmissione trasmissione = TrasmManager.getGestioneTrasmissione(this);
            trasmissione.trasmissioniSingole[idCorrispondente].tipoTrasm = ((DropDownList)sender).SelectedItem.Value;
            trasmissione.trasmissioniSingole[idCorrispondente].daAggiornare = true;
        }
        #endregion

        #region Gestione CallContext

        /// <summary>
        /// Ripristino del contesto chiamante
        /// </summary>
        protected void RestoreCallerContext()
        {
            // Ripristino contesto precedente
            SiteNavigation.CallContextStack.RestoreCaller();

            SiteNavigation.NavigationContext.RefreshNavigation();
        }

        #endregion

        #region Cessione dei diritti sull'oggetto

        /// <summary>
        /// Verifica se esistono RUOLI tra i destinatari della trasmissione 
        /// ed imposta quanti sono
        /// </summary>
        private void verificaRuoliDestInTrasmissione()
        {
            foreach (DocsPAWA.DocsPaWR.TrasmissioneSingola trasm in trasmissione.trasmissioniSingole)
            {
                if (trasm.tipoDest.ToString().ToUpper().Equals("RUOLO"))
                    this.numeroRuoliDestInTrasmissione++;
            }
        }

        /// <summary>
        /// Apre una finestra per selezionare il ruolo a cui cedere i diritti di propriet� sull'oggetto
        /// </summary>
        private void aprePopUpSceltaNuovoProprietario(string tipo)
        {
            utils.CessioneDiritti obj = new DocsPAWA.utils.CessioneDiritti();
            if (!ClientScript.IsStartupScriptRegistered("OpenNewWnd"))
                ClientScript.RegisterStartupScript(this.GetType(), "OpenNewWnd", obj.AprePopUpSceltaNuovoProprietario(tipo));
        }

        /// <summary>
        /// Verifica se � una trasmissione con cessione dei diritti sull'oggetto.
        /// Ritorna True se:
        /// 
        ///     1 - la trasmissione � generata da un modello di trasmissione con cessione diritti (anche se l'utente non � abilitato all'editing ACL)
        ///     2 - la trasmissione prevede gi� cessione dei diritti poich� l'utente (abilitato all'editing ACL) ha selezionato la checkbox "Cedi diritti"
        ///     
        /// </summary>
        /// <returns>True, False</returns>
        private bool cessioneDirittiAbilitato()
        {
            bool retValue = false;

            if (modello != null && modello.CEDE_DIRITTI.Equals("1"))
            {
                if (trasmissione.cessione == null)
                {
                    DocsPaWR.CessioneDocumento cessione = new DocsPAWA.DocsPaWR.CessioneDocumento();
                    cessione.docCeduto = true;

                    //*******************************************************************************************
                    // MODIFICA IACOZZILLI GIORDANO 18/07/2012
                    // Modifica inerente la cessione dei diritti di un doc da parte di un utente non proprietario ma 
                    // nel ruolo del proprietario, in quel caso non posso valorizzare l'IDPEOPLE  con il corrente perch�
                    // il proprietario pu� essere un altro utente del mio ruolo, quindi andrei a generare un errore nella security,
                    // devo quindi controllare che nell'idpeople venga inserito l'id corretto del proprietario.
                    string valoreChiaveCediDiritti = utils.InitConfigurationKeys.GetValue(UserManager.getInfoUtente().idAmministrazione, "FE_CEDI_DIRITTI_IN_RUOLO");
                    if (!string.IsNullOrEmpty(valoreChiaveCediDiritti) && valoreChiaveCediDiritti.Equals("1"))
                    {
                        //Devo istanziare una classe utente.
                        string idProprietario = string.Empty;
                        idProprietario = GetAnagUtenteProprietario();
                        Utente _utproprietario = UserManager.GetUtenteByIdPeople(idProprietario);

                        cessione.idPeople = idProprietario;
                        cessione.idRuolo = UserManager.getInfoUtente(this).idGruppo;
                        cessione.userId = _utproprietario.cognome + " " + _utproprietario.nome;


                        if (cessione.idPeople == null || cessione.idPeople == "")
                            cessione.idPeople = idProprietario;

                        if (cessione.idRuolo == null || cessione.idRuolo == "")
                            cessione.idRuolo = UserManager.getInfoUtente(this).idGruppo;

                        if (cessione.userId == null || cessione.userId == "")
                            cessione.userId = _utproprietario.cognome + " " + _utproprietario.nome;
                    }
                    else
                    {
                        //OLD CODE:
                        cessione.idPeople = UserManager.getInfoUtente(this).idPeople;
                        cessione.idRuolo = UserManager.getInfoUtente(this).idGruppo;
                        cessione.userId = UserManager.getInfoUtente(this).userId;
                    }
                    //*******************************************************************************************
                    // FINE MODIFICA
                    //********************************************************************************************
                    cessione.idPeopleNewPropr = modello.ID_PEOPLE_NEW_OWNER;
                    cessione.idRuoloNewPropr = modello.ID_GROUP_NEW_OWNER;

                    trasmissione.cessione = cessione;
                }
            }
            else
                retValue = (trasmissione.cessione != null && trasmissione.cessione.docCeduto);

            return retValue;
        }

        /// <summary>
        /// Iacozzilli: Faccio la Get dell'idProprietario del doc!
        /// </summary>
        /// <returns></returns>
        private string GetAnagUtenteProprietario()
        {
            DocumentoDiritto[] listaVisibilita = null;
            DocsPaWR.SchedaDocumento sd = DocumentManager.getDocumentoSelezionato(this);
            string idProprietario = string.Empty;
            listaVisibilita = DocumentManager.getListaVisibilitaSemplificata(this, sd.docNumber, true);
            if (listaVisibilita != null && listaVisibilita.Length > 0)
            {
                for (int i = 0; i < listaVisibilita.Length; i++)
                {
                    if (listaVisibilita[i].accessRights == 0)
                    {
                        return idProprietario = listaVisibilita[i].personorgroup;
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Verifica se l'utente � il PROPRIETARIO dell'oggetto
        /// </summary>
        /// <returns>True, False</returns>
        private bool utenteProprietario(string docNumber)
        {

            //Modifica IACOZZILLI GIORDANO 17/07/2012
            //estendo la cessione dei diritti anche a chi non � il proprietario.
            //In questo caso se cedo pi� volte i diritti all'interno dello stesso ruolo
            //in pancia al programma rimane sempre il Primo proprietario, i nuovi non li setta.
            //Devo farlo io
            //
            //OLD CODE:
            //utils.CessioneDiritti obj = new DocsPAWA.utils.CessioneDiritti();
            //return obj.UtenteProprietario(docNumber);
            //
            //NEW CODE:
            string valoreChiaveCediDiritti = utils.InitConfigurationKeys.GetValue(UserManager.getInfoUtente().idAmministrazione, "FE_CEDI_DIRITTI_IN_RUOLO");
            if (!string.IsNullOrEmpty(valoreChiaveCediDiritti) && valoreChiaveCediDiritti.Equals("1"))
            {
                return true;
            }
            else
            {
                utils.CessioneDiritti obj = new DocsPAWA.utils.CessioneDiritti();
                return obj.UtenteProprietario(docNumber);
            }

        }

        /// <summary>
        /// Invia un messaggio a video che avvisa l'utente che tra i destinatari della trasmissioni
        /// non ci sono ruoli
        /// </summary>
        private void inviaMsgNoRuoli()
        {
            utils.CessioneDiritti obj = new DocsPAWA.utils.CessioneDiritti();
            if (!ClientScript.IsStartupScriptRegistered("OpenMsg"))
                ClientScript.RegisterStartupScript(this.GetType(), "OpenMsg", obj.InviaMsgNoRuoli());
        }

        /// <summary>
        /// Verifica se ci sono utenti con notifica, quanti sono e, nel caso di 1, ne memorizza l'ID
        /// </summary>
        private void utentiConNotifica()
        {
            foreach (DocsPAWA.DocsPaWR.TrasmissioneSingola trasm in trasmissione.trasmissioniSingole)
                foreach (DocsPAWA.DocsPaWR.TrasmissioneUtente trasmUt in trasm.trasmissioneUtente)
                    if (trasmUt.daNotificare)
                    {
                        this.numeroUtentiConNotifica++;
                        this.idPeopleNewOwner = trasmUt.utente.idPeople; // memorizza l'id people... da utilizzare nel caso di un solo utente con notifica                        
                    }
        }

        /// <summary>
        /// Imposta il flag di notifica utente
        /// </summary>
        /// <param name="trasmUt"></param>
        /// <returns></returns>
        private bool selezionaNotifica(DocsPaWR.TrasmissioneUtente trasmUt, int indice)
        {
            bool retValue;
            //di default � selezionato (per mantenere la compatibilit� con la vecchia funzionalit�)
            //opzione configurabile da web.config: o tutti selezionati o nessuno.
            //retValue = TrasmManager.getTxRuoloUtentiChecked();

            //reperimento valore flag da hashTable in sessione (se esiste)
            retValue = this.drawTableFlagNotificaUt(indice, trasmUt.utente.idPeople, trasmUt.daNotificare);

            //se esiste la cessione dei diritti, la notifica � legata all'utente (che acquisir� i diritti) impostato precedentemente
            if (trasmissione.cessione != null && trasmissione.cessione.idPeopleNewPropr != null)
                if (trasmUt.utente.idPeople == trasmissione.cessione.idPeopleNewPropr)
                    retValue = true;
                else
                    retValue = false;

            return retValue;
        }

        /// <summary>
        /// Imposta il flag di notifica utente quando la trasmissione � presa da un modello
        /// </summary>
        /// <param name="trasmUt"></param>
        /// <returns></returns>
        private bool selezionaNotificaDaModello(DocsPAWA.DocsPaWR.Corrispondente Corr, DocsPaWR.Utente utente, DocsPaWR.ModelloTrasmissione modello)
        {
            bool retValue = false;

            try
            {
                if (modello.RAGIONI_DESTINATARI != null)
                {
                    foreach (DocsPaWR.RagioneDest rag_dest in modello.RAGIONI_DESTINATARI)
                    {
                        foreach (DocsPaWR.MittDest mitt_dest in rag_dest.DESTINATARI)
                        {
                            if (mitt_dest.CHA_TIPO_URP.Equals("R"))
                            {
                                if (mitt_dest.ID_CORR_GLOBALI.ToString().Equals(Corr.systemId))
                                {
                                    if (mitt_dest.UTENTI_NOTIFICA != null)
                                    {
                                        foreach (DocsPaWR.UtentiConNotificaTrasm utNot in mitt_dest.UTENTI_NOTIFICA)
                                        {
                                            if (utente.idPeople.Equals(utNot.ID_PEOPLE))
                                                return Convert.ToBoolean(utNot.FLAG_NOTIFICA.Replace("1", "true").Replace("0", "false"));
                                        }
                                    }
                                    else
                                    {
                                        // imposta comunque a true
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                retValue = false;
            }

            return retValue;
        }
        #endregion

        private void eliminaTrasmSalvata()
        {
            try
            {
                if (!TrasmManager.deleteTrasm(trasmissione))
                {
                    if (!ClientScript.IsStartupScriptRegistered("errorInDelete"))
                        ClientScript.RegisterStartupScript(this.GetType(), "errorInDelete", "<script>alert('Attenzione, si � verificato un errore durante la rimozione della trasmissione salvata.');</script>");
                }
            }
            catch
            {
                if (!ClientScript.IsStartupScriptRegistered("errorInDelete"))
                    ClientScript.RegisterStartupScript(this.GetType(), "errorInDelete", "<script>alert('Attenzione, si � verificato un errore durante la rimozione della trasmissione salvata.');</script>");
            }

            if (!ClientScript.IsStartupScriptRegistered("redirect"))
                ClientScript.RegisterStartupScript(this.GetType(), "redirect", "<script>redirect();</script>");
        }

        #region Gestione memoria dei flag "Notifica utente"

        /// <summary>
        /// Metodo principale per la gestione della memoria per flag notifiche
        /// </summary>
        private void gestioneMemoriaNotificaUt()
        {
            if (trasmissione != null && !existMemoriaNotificaUt())
                this.createHashNotificaUt();

            if (trasmissione != null && existMemoriaNotificaUt())
                this.verificaAddTrasmS();
        }

        /// <summary>
        /// Verifica se sono state aggiunte delle nuove trasmissioni singole;
        /// in tal caso aggiorna la memoria dei flag delle notifiche
        /// </summary>
        private void verificaAddTrasmS()
        {
            ArrayList listaNotUt = this.getMemoriaNotificaUt();

            if (trasmissione.trasmissioniSingole.Length > listaNotUt.Count)
                addHashNotificaUt();
        }

        /// <summary>
        /// Verifica se esiste una memoria (Session "MEM_NOT_UT") per i flag delle notifiche
        /// </summary>
        /// <returns></returns>
        private bool existMemoriaNotificaUt()
        {
            bool exist = false;

            if (System.Web.HttpContext.Current.Session["MEM_NOT_UT"] != null)
                exist = true;

            return exist;
        }

        /// <summary>
        /// Imposta la memoria (Session "MEM_NOT_UT") per i flag delle notifiche
        /// </summary>
        /// <param name="lista_notifiche_ut">lista delle notifiche</param>
        private void setMemoriaNotificaUt(ArrayList lista_notifiche_ut)
        {
            if (System.Web.HttpContext.Current.Session["MEM_NOT_UT"] == null)
                System.Web.HttpContext.Current.Session.Add("MEM_NOT_UT", lista_notifiche_ut);
        }

        /// <summary>
        /// Reperisce la memoria (Session "MEM_NOT_UT") per i flag delle notifiche
        /// </summary>
        /// <returns></returns>
        private ArrayList getMemoriaNotificaUt()
        {
            ArrayList listaNoticheUt = new ArrayList();
            if (System.Web.HttpContext.Current.Session["MEM_NOT_UT"] != null)
                listaNoticheUt = (ArrayList)System.Web.HttpContext.Current.Session["MEM_NOT_UT"];
            return listaNoticheUt;
        }

        /// <summary>
        /// Rilascia la memoria (Session "MEM_NOT_UT") per i flag delle notifiche
        /// </summary>
        private void releaseMemoriaNotificaUt()
        {
            System.Web.HttpContext.Current.Session.Remove("MEM_NOT_UT");
        }

        /// <summary>
        /// Crea la lista delle notifiche rispetto alle trasmissioni singole
        /// </summary>
        private void createHashNotificaUt()
        {
            Hashtable hashT;
            ArrayList lista = new ArrayList();

            DocsPaWR.Trasmissione trasm = TrasmManager.getGestioneTrasmissione(this);

            if (trasm != null && trasm.trasmissioniSingole != null)
            {
                foreach (DocsPAWA.DocsPaWR.TrasmissioneSingola trasmS in trasm.trasmissioniSingole)
                {
                    hashT = new Hashtable();
                    foreach (DocsPAWA.DocsPaWR.TrasmissioneUtente trasmU in trasmS.trasmissioneUtente)
                    {
                        //ABBATANGELI GIANLUIGI - se esiste gi� un utente in lista genera errore
                        if (!hashT.Contains(trasmU.utente.idPeople))
                            hashT.Add(trasmU.utente.idPeople, trasmU.daNotificare);
                    }
                    lista.Add(hashT);
                }
            }

            if (lista != null && lista.Count > 0)
                this.setMemoriaNotificaUt(lista);
        }

        /// <summary>
        /// Modifica la lista delle notifiche
        /// </summary>
        /// <param name="indiceTrasmSingola">Indice della Trasm.ne Singola</param>
        /// <param name="IDUtente">ID People dell'utente</param>
        /// <param name="valoreFlag">Valore da impostare</param>
        private void modifyHashNotificaUt(int indiceTrasmSingola, string IDUtente, bool valoreFlag)
        {
            ArrayList lista = new ArrayList();
            Hashtable hashT;

            if (existMemoriaNotificaUt())
            {
                lista = this.getMemoriaNotificaUt();
                if (lista != null && lista.Count > 0)
                {
                    hashT = (Hashtable)lista[indiceTrasmSingola];
                    hashT[IDUtente] = valoreFlag;
                }

                this.releaseMemoriaNotificaUt();
                this.setMemoriaNotificaUt(lista);
            }
        }

        /// <summary>
        /// Aggiunge dati alla lista delle notifiche rispetto alle trasmissioni singole
        /// </summary>
        private void addHashNotificaUt()
        {
            Hashtable hashT;
            DocsPAWA.DocsPaWR.TrasmissioneSingola trasmS;

            ArrayList currentList = this.getMemoriaNotificaUt();

            int currentListCount = currentList.Count;
            int currentTrasmSCount = trasmissione.trasmissioniSingole.Length;

            for (int i = currentListCount + 1; i <= currentTrasmSCount; i++)
            {
                trasmS = trasmissione.trasmissioniSingole[i - 1];

                hashT = new Hashtable();
                foreach (DocsPAWA.DocsPaWR.TrasmissioneUtente trasmU in trasmS.trasmissioneUtente)
                {
                    hashT.Add(trasmU.utente.idPeople, trasmU.daNotificare);
                }

                if (hashT != null && hashT.Count > 0)
                    currentList.Add(hashT);
            }

            this.releaseMemoriaNotificaUt();
            this.setMemoriaNotificaUt(currentList);
        }

        /// <summary>
        /// Elimina un item dalla lista delle notifiche
        /// </summary>
        /// <param name="indiceArray">Indice della lista da rimuovere</param>
        private void removeHashNotificaUt(int indiceArray)
        {
            ArrayList currentList = this.getMemoriaNotificaUt();
            currentList.RemoveAt(indiceArray);
            this.releaseMemoriaNotificaUt();
            this.setMemoriaNotificaUt(currentList);
        }

        /// <summary>
        /// Reperisce il valore della notifica rispetto alla chiave passata
        /// </summary>
        /// <param name="indiceArray">Indice dell'item della lista da modificare</param>
        /// <param name="key">chiave da modificare (IdPeople dell'utente della Trasm.ne utente)</param>
        /// <returns></returns>
        private bool getValueHashNotificaUt(int indiceArray, string key)
        {
            bool retValue;

            Hashtable hashT = (Hashtable)this.getMemoriaNotificaUt()[indiceArray];

            retValue = (bool)hashT[key];

            return retValue;
        }

        /// <summary>
        /// Imposta il valore del check nella fase di generazione della tabella delle trasmissioni singole
        /// </summary>
        /// <param name="indiceArray">Indice della lista</param>
        /// <param name="idUtente">IDPeople dell'utente</param>
        /// <param name="currentValue">Valore corrente di default in caso di lista non impostata</param>
        /// <returns></returns>
        private bool drawTableFlagNotificaUt(int indiceArray, string idUtente, bool currentValue)
        {
            bool retValue = currentValue;

            if (this.existMemoriaNotificaUt())
                if (indiceArray <= this.getMemoriaNotificaUt().Count - 1)
                    retValue = this.getValueHashNotificaUt(indiceArray, idUtente);

            return retValue;
        }

        /// <summary>
        /// Riversa i dati della lista delle notifiche in memoria nell'oggetto Trasmisissione in fase di salvataggio dei dati 
        /// </summary>
        private void impostaHashNotificheInObjTrasm()
        {
            int indiceTrasmS = 0;
            DocsPaWR.Trasmissione trasm = TrasmManager.getGestioneTrasmissione(this);

            foreach (DocsPAWA.DocsPaWR.TrasmissioneSingola trasmS in trasm.trasmissioniSingole)
            {
                Hashtable hashT = (Hashtable)this.getMemoriaNotificaUt()[indiceTrasmS];

                if (hashT.Count > 0)
                {
                    foreach (DocsPAWA.DocsPaWR.TrasmissioneUtente trasmU in trasmS.trasmissioneUtente)
                    {
                        trasmU.daNotificare = this.getValueHashNotificaUt(indiceTrasmS, trasmU.utente.idPeople);
                    }
                }

                indiceTrasmS++;
            }
        }

        #endregion
    }
}