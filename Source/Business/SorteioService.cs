using Excel;
using Maistaxi.Business.Model;
using Maistaxi.Business.Model.Publicacao;
using Maistaxi.Business.Pdf;
using System;
using System.IO;

namespace Maistaxi.Business {

    public delegate void SorteioChangedEventHandler(Sorteio s);

    public class SorteioService {

        /* Métodos de Configuração do Database */

        public event SorteioChangedEventHandler SorteioChanged;

        private Sorteio model;
        public Sorteio Model {
            get { return model; }
            set { model = value; SorteioChanged(model); }
        }

        public SorteioService() {
            Database.Initialize();
        }

        private void Execute(Action<Database> action) {
            using (System.Data.SQLite.SQLiteConnection connection = Database.CreateConnection()) {
                using (System.Data.SQLite.SQLiteTransaction tx = connection.BeginTransaction()) {
                    Database database = new Database(connection, tx);
                    try {
                        action(database);
                        tx.Commit();
                    } catch(Exception e) {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public void ExcluirBancoReiniciarAplicacao() {
            Database.ExcluirBanco();
            System.Windows.Application.Current.Shutdown();
        }

        /* Ações sobre as Listas de Sorteios */

        public void AtualizarListas()
        {
            Execute(d => {
                d.AtualizarListas(Model.Listas);
                AtualizarStatusSorteio(d, Status.SORTEIO);
            });
        }

        private void AtualizarStatusSorteio(Database database, string status) {
            Model.StatusSorteio = status;
            database.AtualizarStatusSorteio(status);
        }

        public void AtualizarSorteio() {
            Execute(d => {
                d.AtualizarSorteio(Model);
                AtualizarStatusSorteio(d, Status.IMPORTACAO);
            });
        }

        public int CandidatosDisponiveisLista(int idLista)
        {
            int qtdCandidatos = 0;
            Execute(d => {
                qtdCandidatos = d.CandidatosDisponiveisLista(idLista);
            });
            return qtdCandidatos;
        }

        public void CarregarListas() {
            Execute(d => {
                Model.Listas = d.CarregarListas();
            });
            
        }

        public void CarregarListaProxima() {
            Execute(d => {
                Model.ProximaLista = d.CarregarProximaLista();
            });
        }

        public void CarregarSorteio()
        {
            Execute(d => {
                Model = d.CarregarSorteio();
            });
        }

        public int ContagemCandidatos() {
            int contagemCandidatos = 0;
            Execute(d => {
                contagemCandidatos = d.ContagemCandidatos();
            });
            return contagemCandidatos;
        }

        public void CriarListasSorteioDeFaixas(string arquivoImportacao, string faixa, Action<string> updateStatus, Action<int> updateProgress, int listaAtual, int totalListas, int incremento)
        {
            if (arquivoImportacao != null)
            {
                Execute(d =>
                {
                    using (Stream stream = File.OpenRead(arquivoImportacao))
                    {
                        using (IExcelDataReader excelReader = CreateExcelReader(arquivoImportacao, stream))
                        {
                            string cabecalho = String.Concat(excelReader.AsDataSet().Tables[0].Rows[0].ItemArray);
                            string validacaoCabecalho = d.ValidarCabecalho(cabecalho);

                            if (validacaoCabecalho.Length == 0)
                            {
                                excelReader.IsFirstRowAsColumnNames = true;
                            } else
                            {
                                throw new Exception(validacaoCabecalho);
                            }

                            System.Data.DataSet plan = excelReader.AsDataSet().Copy();
                            System.Data.DataTableReader planReader = new System.Data.DataTableReader(plan.Tables[0]);
                            d.CopiarCandidatosArquivo(faixa, planReader, updateStatus, updateProgress);
                        }
                    }
                });
            }
            Execute(d => {
                d.CriarListasSorteioPorFaixa(faixa, updateStatus, updateProgress, listaAtual, totalListas, incremento);
                AtualizarStatusSorteio(d, Status.QUANTIDADES);
            });
        }

        private IExcelDataReader CreateExcelReader(string arquivoImportacao, Stream stream) {
            return (arquivoImportacao.ToLower().EndsWith(".xlsx") ? ExcelReaderFactory.CreateOpenXmlReader(stream) : ExcelReaderFactory.CreateBinaryReader(stream));
        }

        public string DiretorioExportacaoCSV => Database.DiretorioExportacaoCSV;
        public bool DiretorioExportacaoCSVExistente => Directory.Exists(Database.DiretorioExportacaoCSV);

        public void ExportarListas(Action<string> updateStatus, string caminhoArquivo = null)
        {
            Execute(d => {
                d.ExportarListas(updateStatus, caminhoArquivo);
            });
        }

        public void SalvarLista(Lista lista, string caminhoArquivo) {
            ListaPub listaPublicacao = null;
            Execute(d => { listaPublicacao = d.CarregarListaPublicacao(lista.IdLista); });
            PdfFileWriter.WriteToPdf(caminhoArquivo, Model, listaPublicacao);
            System.Diagnostics.Process.Start(caminhoArquivo);
        }

        public void SalvarSorteados(string caminhoArquivo)
        {
            ListaPub listaPublicacao = null;
            Execute(d => { listaPublicacao = d.CarregarListaSorteados(); });
            PdfFileWriter.WriteSorteadosToPdf(caminhoArquivo, Model, listaPublicacao);
            System.Diagnostics.Process.Start(caminhoArquivo);
        }

        public bool SortearProximaLista(Action<string> updateStatus, Action<int> updateProgress, Action<string, bool> logText, int? sementePersonalizada = null)
        {
            Lista listaSorteada = null;
            Lista listaAtual = new Lista { IdLista = model.ProximaLista.IdLista };

            Execute(d => {
                listaSorteada = d.SortearProximaLista(updateStatus, updateProgress, logText, sementePersonalizada);
                if (listaSorteada != null)
                {
                    if (Model.StatusSorteio == Status.SORTEIO)
                    {
                        AtualizarStatusSorteio(d, Status.SORTEIO_INICIADO);
                    }
                    if (d.CarregarProximaLista() == null)
                    {
                        AtualizarStatusSorteio(d, Status.FINALIZADO);
                    }
                }
            });

            if (listaSorteada != null)
            {
                String pasta = System.Configuration.ConfigurationManager.AppSettings.Get("PASTA_RESULTADO");
                if (!Directory.Exists(pasta))
                {
                    Directory.CreateDirectory(pasta);
                }
                if (listaSorteada.IdLista > 0)
                {
                    SalvarLista(listaSorteada, (String.Concat(pasta, listaSorteada.OrdemSorteio.ToString("00"), " - ", listaSorteada.Nome.Split('%')[0], ".pdf")));
                }
                else
                {
                    SalvarLista(model.ProximaLista, (String.Concat(pasta, model.ProximaLista.OrdemSorteio.ToString("00"), " - ", model.ProximaLista.Nome.Split('%')[0], ".pdf")));
                }
            }

            return listaSorteada != null;
        }
    }
}
