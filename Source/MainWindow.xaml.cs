using Maistaxi.Business;
using Maistaxi.Business.Model;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Maistaxi {
    /// <summary>
    /// Métodos de Manipulação dos componentes da tela de MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private bool activated = false;
        private bool processing = false;

        private SorteioService Service { get; set; }
        private Sorteio Sorteio => Service.Model;
        private string StatusSorteio => Sorteio.StatusSorteio;

        public MainWindow() {

            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Title += $" v{versionInfo.FileVersion}";

            Service = new SorteioService();
            Service.SorteioChanged += (s) => { DataContext = s; };
            Service.CarregarSorteio();

            EtapaCadastro(false);
            EtapaImportacao(false);
            EtapaQuantidades(false);
            EtapaSorteio(false);
            EtapaFinalizado(false);
        }

        private void Window_Activated(object sender, EventArgs e) {
            if (!activated) {
                activated = true;
                switch (StatusSorteio) {
                    case Status.CADASTRO:
                        EtapaCadastro(true);
                        break;
                    case Status.IMPORTACAO:
                        EtapaImportacao(true);
                        break;
                    case Status.QUANTIDADES:
                        EtapaQuantidades(true);
                        break;
                    case Status.SORTEIO:
                    case Status.SORTEIO_INICIADO:
                        EtapaSorteio(true);
                        break;
                    case Status.FINALIZADO:
                        EtapaFinalizado(true);
                        break;
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            e.Cancel = processing;
        }

        private bool VerificarStatus(params string[] statuses) {
            return statuses.Contains(StatusSorteio);
        }

        private void AlternarTab(TabItem tab, bool ativo) {
            if (ativo) {
                tab.Visibility = Visibility.Visible;
                tab.Focus();
                lblEtapaSorteio.Content = tab.Header;
            }
            (tab.Content as Grid).IsEnabled = ativo;
            tab.Visibility = Visibility.Collapsed;
        }

        private void ShowMessage(string message) {
            MessageBox.Show(
                message,
                "AVISO",
                MessageBoxButton.OK,
                MessageBoxImage.Asterisk
            );
        }

        private void ShowErrorMessage(string message) {
            MessageBox.Show(
                message,
                "ERRO",
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation
            );
        }

        /* Ativação das etapas do sorteio. */
        private void EtapaCadastro(bool ativo) {
            Service.CarregarSorteio();

            if (VerificarStatus(Status.CADASTRO))
            {
                Service.Model.Nome = String.Concat(Service.Model.Nome, " - ", DateTime.Now.ToString("MMMM/yyyy")).ToUpper();
            }
            btnAvancarCadastro.IsEnabled = !VerificarStatus(Status.CADASTRO);
            grdFormCadastro.IsEnabled = VerificarStatus(Status.CADASTRO, Status.IMPORTACAO);
            AlternarTab(tabCadastro, ativo);
        }

        private void EtapaImportacao(bool ativo) {
            btnRecuarImportacao.IsEnabled = true;
            btnAvancarImportacao.IsEnabled = !VerificarStatus(Status.CADASTRO, Status.IMPORTACAO);
            grdArquivoImportacao.IsEnabled = VerificarStatus(Status.IMPORTACAO);
            grdConfiguracaoImportacao.IsEnabled = true;
            grdImportacaoEmAndamento.IsEnabled = false;
            if (!VerificarStatus(Status.IMPORTACAO)) {
                btnImportarArquivo.IsEnabled = false;
                if (ativo) {
                    lblStatusImportacao.Content = $"{Service.ContagemCandidatos()} candidatos importados.";
                }
            }
            AlternarTab(tabImportacao, ativo);
        }

        private void EtapaQuantidades(bool ativo) {
            Service.CarregarListas();
            lstQuantidades.IsEnabled = VerificarStatus(Status.QUANTIDADES, Status.SORTEIO);
            btnAtualizarQuantidades.IsEnabled = VerificarStatus(Status.QUANTIDADES, Status.SORTEIO);
            btnAvancarQuantidades.IsEnabled = !VerificarStatus(Status.CADASTRO, Status.IMPORTACAO, Status.QUANTIDADES);
            AlternarTab(tabQuantidades, ativo);
        }

        private void EtapaSorteio(bool ativo) {
            Service.CarregarListas();
            Service.CarregarListaProxima();
            btnRecuarSorteio.IsEnabled = true;
            btnAvancarSorteio.IsEnabled = VerificarStatus(Status.FINALIZADO);
            grdIniciarSorteio.IsEnabled = VerificarStatus(Status.SORTEIO, Status.SORTEIO_INICIADO);
            grdSorteioEmAndamento.IsEnabled = false;
            lstSorteioListasSorteio.IsEnabled = true;
            AlternarTab(tabSorteio, ativo);
        }

        private void EtapaFinalizado(bool ativo) {
            btnRecuarFinalizado.IsEnabled = true;
            btnExportarListas.IsEnabled = true;
            btnAbrirDiretorioExportacao.IsEnabled = ativo && Service.DiretorioExportacaoCSVExistente;
            AlternarTab(tabFinalizado, ativo);
        }

        /* Transição entre as etapas .*/

        private void btnAvancarConfiguracao_Click(object sender, RoutedEventArgs e) {
            EtapaCadastro(true);
        }

        private void buttonAvancarCadastro_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            EtapaCadastro(false);
            EtapaImportacao(true);
        }

        private void buttonRecuarImportacao_Click(object sender, RoutedEventArgs e) {
            EtapaImportacao(false);
            EtapaCadastro(true);
        }

        private void buttonAvancarImportacao_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            EtapaImportacao(false);
            EtapaQuantidades(true);
        }

        private void buttonRecuarQuantidades_Click(object sender, RoutedEventArgs e) {
            EtapaQuantidades(false);
            EtapaImportacao(true);
        }

        private void buttonAvancarQuantidades_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            EtapaQuantidades(false);
            EtapaSorteio(true);
        }

        private void buttonRecuarSorteio_Click(object sender, RoutedEventArgs e) {
            EtapaSorteio(false);
            EtapaQuantidades(true);
        }

        private void buttonAvancarSorteio_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            EtapaSorteio(false);
            EtapaFinalizado(true);
        }

        private void buttonRecuarFinalizado_Click(object sender, RoutedEventArgs e) {
            EtapaFinalizado(false);
            EtapaSorteio(true);
        }

        /* Etapa de Configuração */

        private void btnExcluirDados_Click(object sender, RoutedEventArgs e) {

            MessageBoxResult result = MessageBox.Show(
                $"Excluir dados e reiniciar aplicação?",
                "Excluir dados?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes) {
                MessageBoxResult confirmResult = MessageBox.Show(
                    $"Tem certeza? A exclusão dos dados é definitiva!",
                    "Excluir dados?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (confirmResult == MessageBoxResult.Yes) {
                    Service.ExcluirBancoReiniciarAplicacao();
                }
            }
        }

        /* Etapa de Cadastro. */
        private void buttonAtualizarCadastro_Click(object sender, RoutedEventArgs e) {
            if (Sorteio.IsValid) {
                Service.AtualizarSorteio();
                EtapaCadastro(true);
                ShowMessage("Sorteio Alterado!");
            }
        }

        private void RegistrarPastaResultado()
        {
            string pastaResultadoAtual = System.Configuration.ConfigurationManager.AppSettings.Get("PASTA_RESULTADO");
            if (String.IsNullOrWhiteSpace(pastaResultadoAtual))
            {
                string[] divisorPasta = { "\\" };
                string[] caminhoArquivoCandidatos = arqInscritos.Text.Split(divisorPasta, StringSplitOptions.None);
                caminhoArquivoCandidatos[caminhoArquivoCandidatos.Count() - 1] = "";
                string pastaResultado = String.Join("\\", caminhoArquivoCandidatos);
                System.Configuration.ConfigurationManager.AppSettings.Set("PASTA_RESULTADO", pastaResultado);
            }
        }

        /* Etapa de Importação. */
        private string GetDragEventFile(DragEventArgs e) {

            string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop, false);

            bool validFile = files != null
                && files.Count() == 1
                && (files.First().ToLower().EndsWith(".xls") || files.First().ToLower().EndsWith(".xlsx"));

            return (validFile) ? files.First() : null;
        }

        private void AtribuirArquivoImportacao(string file) {
            lblNomeArquivo.Content = file.Split(new char[] { '\\', '/' }).Last();
            lblCaminhoArquivo.Content = file;
            imgArquivoSelecionado.Visibility = Visibility.Visible;
            imgSemArquivo.Visibility = Visibility.Hidden;
            btnImportarArquivo.IsEnabled = true;
        }

        private void LimparArquivoImportacao() {
            lblNomeArquivo.Content = "";
            lblCaminhoArquivo.Content = "";
            imgArquivoSelecionado.Visibility = Visibility.Hidden;
            imgSemArquivo.Visibility = Visibility.Visible;
            btnImportarArquivo.IsEnabled = false;
        }
        
        private void gridImportacao_DragEnter(object sender, DragEventArgs e) {
            if (GetDragEventFile(e) != null) {
                imgCerto.Visibility = Visibility.Visible;
            } else {
                imgErrado.Visibility = Visibility.Visible;
            }
        }

        private void gridImportacao_DragLeave(object sender, DragEventArgs e) {
            imgErrado.Visibility = Visibility.Hidden;
            imgCerto.Visibility = Visibility.Hidden;
        }

        private void gridArquivoImportacao_Drop(object sender, DragEventArgs e) {
            imgErrado.Visibility = Visibility.Hidden;
            imgCerto.Visibility = Visibility.Hidden;
            string file = GetDragEventFile(e);
            if (GetDragEventFile(e) != null) {
                AtribuirArquivoImportacao(file);
            } else {
                LimparArquivoImportacao();
            }
        }

        private void gridArquivoImportacao_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".xlsx";
            fileDialog.Filter = "Planilha Excel (*.xlsx)|*.xlsx|Planilha Excel 97-2003 (*.xls)|*.xls";

            bool? result = fileDialog.ShowDialog();
            if (result == true) {
                AtribuirArquivoImportacao(fileDialog.FileName);
            }
        }

        private void gridArquivoImportacaoFaixaA(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".xlsx";
            fileDialog.Filter = "Planilha Excel (*.xlsx)|*.xlsx|Planilha Excel 97-2003 (*.xls)|*.xls";

            bool? result = fileDialog.ShowDialog();
            if (result == true)
            {
                btnAtualizarCadastro.IsEnabled = true;
                arqInscritos.Text = fileDialog.FileName;
                btnImportarArquivo.IsEnabled = true;
            }
        }

        private void buttonImportarArquivo_Click(object sender, RoutedEventArgs e)
        {
            btnRecuarImportacao.IsEnabled = false;
            btnAvancarImportacao.IsEnabled = false;
            grdConfiguracaoImportacao.IsEnabled = false;
            grdImportacaoEmAndamento.IsEnabled = true;

            string caminhoArquivoCandidatos = arqInscritos.Text.Contains("\\") ? arqInscritos.Text : null;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;

                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusImportacao.Content = value; });
                Action<int> updateProgress = (value) => Dispatcher.Invoke(() => { pgrImportacao.Value = value; });

                try
                {
                    Service.CriarListasSorteioDeFaixas(caminhoArquivoCandidatos, "Veículo Especial Adaptado", updateStatus, updateProgress, 1, 12, 0);
                    Service.CriarListasSorteioDeFaixas(null, "Veículo Especial Adaptado SUPLENTES", updateStatus, updateProgress, 7, 12, 3);
                    Service.CriarListasSorteioDeFaixas(null, "Veículo Híbrido", updateStatus, updateProgress, 3, 12, 6);
                    Service.CriarListasSorteioDeFaixas(null, "Veículo Híbrido SUPLENTES", updateStatus, updateProgress, 9, 12, 9);
                    Service.CriarListasSorteioDeFaixas(null, "Veículo Comum", updateStatus, updateProgress, 5, 12, 12);
                    Service.CriarListasSorteioDeFaixas(null, "Veículo Comum SUPLENTES", updateStatus, updateProgress, 11, 12, 15);

                    updateStatus("Importação finalizada.");
                }
                catch (Exception exception)
                {
                    ShowErrorMessage($"Erro na importação: {exception.Message}");
                    updateProgress(0);
                    updateStatus("Erro na importação.");
                }

                Dispatcher.Invoke(() => EtapaImportacao(true));
                processing = false;
            };
            worker.RunWorkerAsync();
        }

        /* Etapa de Quantidades. */

        private void buttonAtualizarQuantidades_Click(object sender, RoutedEventArgs e) {
            foreach (Lista lista in Sorteio.Listas)
            {
                int qtdCandidatos = Service.CandidatosDisponiveisLista(lista.IdLista);
                if (qtdCandidatos < lista.Quantidade)
                {
                    btnAvancarQuantidades.IsEnabled = false;
                    ShowErrorMessage(String.Concat("A Lista ", lista.OrdemSorteio, " - ", lista.Nome, " possui mais autorizações (", lista.Quantidade," autoriza", lista.Quantidade > 1 ? "ções" : "ção", ") do que candidatos (", qtdCandidatos, " motorista", qtdCandidatos > 1 ? "s" : "", ")."));
                    return;
                }
            }
            if (Sorteio.Listas.All(l => l.IsValid)) {
                Service.AtualizarListas();
                ShowMessage("Quantidade de autorizações por lista atualizadas!");
                EtapaQuantidades(true);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            (sender as TextBox).SelectAll();
        }

        /* Etapa de Sorteio. */
        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            txtSementePersonalizada.IsEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            txtSementePersonalizada.Clear();
            txtSementePersonalizada.IsEnabled = false;
        }

        private void BloquearEtapaSorteio() {

            btnRecuarSorteio.IsEnabled = false;
            btnAvancarSorteio.IsEnabled = false;
            grdIniciarSorteio.IsEnabled = false;
            grdSorteioEmAndamento.IsEnabled = true;
            lstSorteioListasSorteio.IsEnabled = false;

            lblAnimacao.Visibility = Visibility.Visible;
            lblColunas.Visibility = Visibility.Visible;
            lblMunicipe.Visibility = Visibility.Visible;
            lblSorteioListaAtual.Visibility = Visibility.Visible;
            lblSorteioProximaLista.Visibility = Visibility.Hidden;
        }

        private void buttonSortearProximaLista_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            int? sementePersonalizada = null;
            if (chkSementePersonalizada.IsChecked == true) {
                int valorSemente;
                if (!int.TryParse(txtSementePersonalizada.Text.Trim(), out valorSemente)) {
                    ShowErrorMessage("O valor de semente informado é inválido.");
                    return;
                } else {
                    sementePersonalizada = valorSemente;
                }
            }

            BloquearEtapaSorteio();
            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {
                processing = true;
                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusSorteio.Content = value; });
                Action<int> updateProgress = (value) => Dispatcher.Invoke(() => { pgrSorteio.Value = value; });
                Action<string, bool> logText = (value, substituir) => Dispatcher.Invoke(() => {

                    switch (lblAnimacao.Content.ToString().Length)
                    {
                        case 1:
                            lblAnimacao.Content = String.Concat(lblAnimacao.Content, ConsultarUnicode("0xEC37"));
                            break;
                        case 2:
                            lblAnimacao.Content = String.Concat(lblAnimacao.Content, ConsultarUnicode("0xEC38"));
                            break;
                        case 3:
                            lblAnimacao.Content = String.Concat(lblAnimacao.Content, ConsultarUnicode("0xEC39"));
                            break;
                        case 4:
                            lblAnimacao.Content = String.Concat(lblAnimacao.Content, ConsultarUnicode("0xEC3A"));
                            break;
                        case 5:
                            lblAnimacao.Content = String.Concat(lblAnimacao.Content, ConsultarUnicode("0xEC3B"));
                            break;
                        case 60:
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xE811"), ConsultarUnicode("0xE81F") + "   ");
                            break;
                        case 70:
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xEC37"), ConsultarUnicode("0xE948"));
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xEC38"), ConsultarUnicode("0xE8E7"));
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xEC39"), ConsultarUnicode("0xE947"));
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xEC3A"), ConsultarUnicode("0xE8DB"));
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xEC3B"), ConsultarUnicode("0xEA37"));
                            lblAnimacao.Content = lblAnimacao.Content.ToString().Replace(ConsultarUnicode("0xE81F"), ConsultarUnicode("0xE7EC") + "   ");
                            break;
                        default:
                            lblAnimacao.Content = lblAnimacao.Content + ConsultarUnicode("0xEC3C");
                            break;
                    }
                    if (substituir)
                    {
                        lblNomeSorteado.HorizontalContentAlignment = HorizontalAlignment.Left;
                        lblNomeSorteado.Content = value;
                        if (value.Length >= 75)
                        {
                            lblNomeSorteado.FontSize = 28;
                        }
                        else
                        {
                            if (value.Length > 50)
                            {
                                lblNomeSorteado.FontSize = 34;
                            }
                            else
                            {
                                lblNomeSorteado.FontSize = 38;
                            }
                        }
                    }
                    else
                    {
                        if (lblAnimacao.Content.ToString().Length > 60)
                        {
                            lblAnimacao.Content = ConsultarUnicode("0xE811");
                        }
                        if (!string.IsNullOrWhiteSpace(txtLogSorteio.Text))
                        {
                            txtLogSorteio.AppendText(Environment.NewLine);
                        }
                        txtLogSorteio.AppendText(value);
                        txtLogSorteio.ScrollToEnd();
                    }
                });

                if (Service.SortearProximaLista(updateStatus, updateProgress, logText, sementePersonalizada))
                {
                    DateTime momento = DateTime.Now;
                    DateTime momentoFinal = DateTime.Now.AddMilliseconds(10000);
                    while(momento < momentoFinal)
                    {
                        momento = DateTime.Now;
                    }
                    Dispatcher.Invoke(() => { txtLogSorteio.Clear(); lblNomeSorteado.Content = ""; txtSementePersonalizada.Text = ""; });
                    processing = false;
                }
                Dispatcher.Invoke(() => EtapaSorteio(true));
            };
            worker.RunWorkerAsync();
        }

        private void btnSalvarLista_Click(object sender, RoutedEventArgs e) {

            Lista lista = ((sender as Button).Parent as Grid).DataContext as Lista;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = lista.Nome;
            saveDialog.DefaultExt = ".pdf";
            saveDialog.Filter = "PDF files (.pdf)|*.pdf";

            if (saveDialog.ShowDialog() == true) {
                Service.SalvarLista(lista, saveDialog.FileName);
            }
        }

        /* Etapa de Sorteio Finalizado. */
        private void buttonExportarListas_Click(object sender, RoutedEventArgs e) {
            RegistrarPastaResultado();
            string caminhoPasta = System.Configuration.ConfigurationManager.AppSettings.Get("PASTA_RESULTADO");
            Service.SalvarSorteados(String.Concat(caminhoPasta, "Sorteados_Contemplados.pdf"));

            btnRecuarFinalizado.IsEnabled = false;
            btnExportarListas.IsEnabled = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (wSender, wE) => {

                processing = true;

                Action<string> updateStatus = (value) => Dispatcher.Invoke(() => { lblStatusExportacao.Content = value; });

                try {
                    Service.ExportarListas(updateStatus);
                    Service.ExportarListas(updateStatus, caminhoPasta);
                    btnAbrirDiretorioExportacaoSelecionado(caminhoPasta);
                    updateStatus("Exportação finalizada.");
                } catch (Exception exception) {
                    ShowErrorMessage($"Erro na exportação: {exception.Message}");
                    updateStatus("Erro na exportação.");
                }

                Dispatcher.Invoke(() => EtapaFinalizado(true));
                processing = false;
            };
            worker.RunWorkerAsync();
        }

        /* Publicação */
        private void btnAbrirDiretorioExportacao_Click(object sender, RoutedEventArgs e) {
            Process.Start(new ProcessStartInfo() {
                FileName = Service.DiretorioExportacaoCSV,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void btnAbrirDiretorioExportacaoSelecionado(string caminhoArquivo)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = caminhoArquivo,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        /* Conversão de Caracteres Especiais Unicode para Animação Lúdica */
        private String ConsultarUnicode(string charHexa)
        {
            uint conversorHexa = System.Convert.ToUInt32(charHexa, 16);
            Char charUnicode = Convert.ToChar(conversorHexa);
            return charUnicode.ToString();
        }
    }
}
