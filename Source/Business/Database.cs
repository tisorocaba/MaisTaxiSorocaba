using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Data.SQLite;
using Maistaxi.Business.Model;
using Maistaxi.Business.Model.Publicacao;

namespace Maistaxi.Business
{
    public class Database
    {
        /* Parametrização de Banco de Dados */
        private static string ConnectionString { get; set; }
        private static SQLiteConnection Connection { get; set; }
        private static SQLiteTransaction Transaction { get; set; }

        string[] listaBooleano = { "SIM", "NAO", "NÃO" };

        public static void Initialize()
        {
            string dbFile = ConfigurationManager.AppSettings["ARQUIVO_BANCO"];
            string dbDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = $"{dbDirectory}{dbFile}";

            SQLiteConnectionStringBuilder stringConexao = new SQLiteConnectionStringBuilder();
            stringConexao.DataSource = dbPath;
            stringConexao.Version = 3;
            stringConexao.DefaultTimeout = 120;

            ConnectionString = stringConexao.ConnectionString;

            SQLiteConnection engine = new SQLiteConnection(ConnectionString);
            Connection = engine.OpenAndReturn();
            string scriptFile = ConfigurationManager.AppSettings["ARQUIVO_SCRIPT"];
            string scriptPath = $"{dbDirectory}{scriptFile}";
            string scriptText;
            using (StreamReader streamReader = new StreamReader(scriptPath, Encoding.UTF8))
            {
                scriptText = streamReader.ReadToEnd();
            }
            using (SQLiteConnection connection = Connection)
            {
                foreach (string commandText in scriptText.Split(';'))
                {
                    if (!string.IsNullOrWhiteSpace(commandText))
                    {
                        using (SQLiteCommand command = new SQLiteCommand())
                        {
                            command.Connection = connection;
                            command.CommandType = CommandType.Text;
                            command.CommandText = commandText;
                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void AtualizarParametro(string nome, string valor)
        {
            ExecuteNonQuery(
                "UPDATE PARAMETRO SET VALOR = @VALOR WHERE NOME = @NOME",
                new SQLiteParameter("NOME", nome) { DbType = DbType.String },
                new SQLiteParameter("VALOR", (object)valor ?? DBNull.Value) { DbType = DbType.String }
            );
        }


        public static SQLiteConnection CreateConnection()
        {
            SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public Database(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        private SQLiteCommand CreateCommand(string commandText, params SQLiteParameter[] parameters)
        {
            SQLiteCommand command = new SQLiteCommand
            {
                Connection = Connection,
                Transaction = Transaction,
                CommandType = CommandType.Text,
                CommandText = commandText
            };
            command.Parameters.AddRange(parameters);
            return command;
        }

        private string CarregarParametro(string nome)
        {
            using (SQLiteCommand command = CreateCommand("SELECT VALOR FROM PARAMETRO WHERE NOME = @NOME"))
            {
                command.Parameters.AddWithValue("@NOME", nome);
                return command.ExecuteScalar() as string;
            }
        }

        public static void ExcluirBanco()
        {
            string dbFile = ConfigurationManager.AppSettings["ARQUIVO_BANCO"];
            string dbDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = $"{dbDirectory}{dbFile}";
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }

        private void ExecuteNonQuery(string commandText, params SQLiteParameter[] parameters)
        {
            using (SQLiteCommand command = CreateCommand(commandText, parameters))
            {
                object column = command.ExecuteScalar();
            }
        }

        private int ExecuteScalar(string commandText, params SQLiteParameter[] parameters)
        {
            using (SQLiteCommand command = CreateCommand(commandText, parameters))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        /* Ações referentes ao sorteio */

        public void AtualizarListaSorteada(Lista lista, Action<string> updateStatus)
        {
            bool sorteada = true;
            ExecuteNonQuery("UPDATE LISTA SET SORTEADA = @SORTEADA WHERE ID_LISTA = @ID_LISTA",
                new SQLiteParameter("ID_LISTA", lista.IdLista) { DbType = DbType.Int32 },
                new SQLiteParameter("SORTEADA", sorteada) { DbType = DbType.Boolean }
            );
            updateStatus("Sorteio Lista " + lista.OrdemSorteio.ToString("00") + " - " + lista.Nome + " finalizado!");
        }

        public void AtualizarSorteio(Sorteio sorteio)
        {
            AtualizarParametro("NOME_SORTEIO", sorteio.Nome);
            AtualizarParametro("INSCRITOS", sorteio.Inscritos);
        }

        public void AtualizarStatusSorteio(string status)
        {
            AtualizarParametro("STATUS_SORTEIO", status);
        }
        private string AnonimizarCpf(object valor)
        {
            double qtdNumeros = 0;
            string[] parte = valor.ToString().Split('.', '-');
            StringBuilder cpfNaoFormatado = new StringBuilder(String.Join("", parte));

            if (cpfNaoFormatado.Length > 11)
            {
                throw new Exception("CPF inválido");
            }
            if (cpfNaoFormatado.Length < 11 || parte.Length < 4)
            {
                int complemento = 11 - cpfNaoFormatado.Length;
                if (complemento > 0)
                {
                    cpfNaoFormatado.Insert(0, "X", complemento);
                }
                parte = new string[4];
                parte[0] = cpfNaoFormatado.ToString().Substring(0, 3);
                parte[1] = cpfNaoFormatado.ToString().Substring(3, 3);
                parte[2] = cpfNaoFormatado.ToString().Substring(6, 3);
                parte[3] = cpfNaoFormatado.ToString().Substring(9, 2);
            }

            for (int ix = 0; ix < 11; ix++)
            {
                if (cpfNaoFormatado.ToString()[ix] >= '0' && cpfNaoFormatado.ToString()[ix] <= '9')
                {
                    qtdNumeros++;
                }
            }

            double percentualNumeros = qtdNumeros / 11.0;

            if (percentualNumeros > 0.55)
            {
                double qtdAnonimo = 11 - qtdNumeros;
                int index = 0;

                while(qtdAnonimo < 5 && index < 11)
                {
                    switch ((int)qtdAnonimo)
                    {
                        case 3:
                            if (cpfNaoFormatado.ToString()[9] >= '0' && cpfNaoFormatado.ToString()[9] <= '9')
                            {
                                index = 9;
                            }
                            break;
                        case 4:
                            if (cpfNaoFormatado.ToString()[10] >= '0' && cpfNaoFormatado.ToString()[10] <= '9')
                            {
                                index = 10;
                            }
                            break;
                    }
                    if (cpfNaoFormatado.ToString()[index] >= '0' && cpfNaoFormatado.ToString()[index] <= '9')
                    {
                        cpfNaoFormatado.Replace(cpfNaoFormatado.ToString()[index], 'X', index, 1);
                        qtdAnonimo++;
                    }
                    index++;
                }
            }

            string cpf = cpfNaoFormatado.ToString();
            return String.Concat(cpf.Substring(0, 3), ".", cpf.Substring(3, 3), ".", cpf.Substring(6, 3), "-", cpf.Substring(9, 2));
        }

        public void AtualizarListas(ICollection<Lista> listas)
        {
            foreach (Lista lista in listas)
            {
                ExecuteNonQuery(
                    "UPDATE LISTA SET QUANTIDADE = @QUANTIDADE WHERE ID_LISTA = @ID_LISTA",
                    new SQLiteParameter("ID_LISTA", lista.IdLista) { DbType = DbType.Int32 },
                    new SQLiteParameter("QUANTIDADE", lista.Quantidade) { DbType = DbType.Int32 }
                );
            }
        }

        public int CandidatosDisponiveisLista(int idLista)
        {
            return ExecuteScalar($"SELECT COUNT(*) FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO WHERE CANDIDATO.CONTEMPLADO = 0 AND CANDIDATO_LISTA.ID_LISTA = {idLista}");
        }

        public ListaPub CarregarListaPublicacao(int idLista)
        {

            ListaPub lista;

            using (SQLiteCommand command = CreateCommand("SELECT ID_LISTA, ORDEM_SORTEIO, NOME, FONTE_SEMENTE, SEMENTE_SORTEIO FROM LISTA WHERE ID_LISTA = @ID_LISTA"))
            {
                command.Parameters.AddWithValue("ID_LISTA", idLista);
                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    resultSet.Read();
                    lista = new ListaPub()
                    {
                        IdLista = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ORDEM_SORTEIO")]),
                        Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                        FonteSementeSorteio = resultSet.GetString(resultSet.GetOrdinal("FONTE_SEMENTE")),
                        SementeSorteio = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEMENTE_SORTEIO")]),
                        Candidatos = new List<CandidatoPub>()
                    };
                }
            }

            string queryCandidatos = @"
                SELECT
                    CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO, CANDIDATO.CPF, CANDIDATO.NOME, CANDIDATO.INSCRICAO
                FROM
                    CANDIDATO_LISTA
                    INNER JOIN LISTA ON CANDIDATO_LISTA.ID_LISTA = LISTA.ID_LISTA
                    INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO IS NOT NULL
                ORDER BY CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO
            ";

            using (SQLiteCommand command = CreateCommand(queryCandidatos))
            {
                command.Parameters.AddWithValue("ID_LISTA", idLista);
                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    while (resultSet.Read())
                    {
                        lista.Candidatos.Add(new CandidatoPub
                        {
                            IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEQUENCIA_CONTEMPLACAO")]),
                            Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            QuantidadeCriterios = 0,
                            IdInscricao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("INSCRICAO")])
                        });
                    }
                }
            }

            return lista;
        }

        public ListaPub CarregarListaSorteados()
        {

            ListaPub lista;
            List<int> idsListasReservas = new List<int>();

            using (SQLiteCommand command = CreateCommand("SELECT ID_LISTA FROM LISTA WHERE NOME LIKE '%SUPLENTE%'"))
            {
                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    while (resultSet.Read())
                    {
                        idsListasReservas.Add(Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_LISTA")]));
                    }
                }
            }

            string listasReservas = String.Join(",", idsListasReservas);

            lista = new ListaPub()
            {
                IdLista = 0,
                Nome = "",
                FonteSementeSorteio = "",
                SementeSorteio = 0,
                Candidatos = new List<CandidatoPub>()
            };

            string queryCandidatos = @"
                SELECT
                    CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO, CANDIDATO.CPF, CANDIDATO.NOME, CANDIDATO.INSCRICAO
                FROM
                    CANDIDATO_LISTA
                    INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO IS NOT NULL
                AND CANDIDATO_LISTA.ID_LISTA NOT IN (" + listasReservas + @")
                ORDER BY CANDIDATO.NOME
            ";

            using (SQLiteCommand command = CreateCommand(queryCandidatos))
            {
                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    while (resultSet.Read())
                    {
                        lista.Candidatos.Add(new CandidatoPub
                        {
                            IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEQUENCIA_CONTEMPLACAO")]),
                            Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            QuantidadeCriterios = 0,
                            IdInscricao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("INSCRICAO")])
                        });
                    }
                }
            }

            return lista;
        }

        public ICollection<Lista> CarregarListas()
        {
            List<Lista> listas = new List<Lista>();
            using (SQLiteCommand command = CreateCommand($"SELECT ID_LISTA, ORDEM_SORTEIO, NOME, QUANTIDADE, SORTEADA, PUBLICADA FROM LISTA ORDER BY ORDEM_SORTEIO"))
            {
                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    while (resultSet.Read())
                    {
                        listas.Add(new Lista
                        {
                            IdLista = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_LISTA")]),
                            OrdemSorteio = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ORDEM_SORTEIO")]),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            Quantidade = Convert.ToInt32(resultSet[resultSet.GetOrdinal("QUANTIDADE")]),
                            Sorteada = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SORTEADA")]) == 1,
                            Publicada = Convert.ToInt32(resultSet[resultSet.GetOrdinal("PUBLICADA")]) == 1,
                        });
                    }
                }
            }
            return listas;
        }

        public Lista CarregarProximaLista()
        {
            using (SQLiteCommand command = CreateCommand("SELECT ID_LISTA, ORDEM_SORTEIO, NOME, QUANTIDADE FROM LISTA WHERE SORTEADA = 0 ORDER BY ORDEM_SORTEIO"))
            {
                using (SQLiteDataReader resultSet = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (resultSet.Read())
                    {
                        int idLista = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_LISTA")]);
                        return new Lista
                        {
                            IdLista = idLista,
                            OrdemSorteio = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ORDEM_SORTEIO")]),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                            Quantidade = Convert.ToInt32(resultSet[resultSet.GetOrdinal("QUANTIDADE")]),
                            CandidatosDisponiveis = CandidatosDisponiveisLista(idLista)
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public Sorteio CarregarSorteio()
        {

            return new Sorteio
            {
                Nome = CarregarParametro("NOME_SORTEIO"),
                StatusSorteio = CarregarParametro("STATUS_SORTEIO"),
                Inscritos = CarregarParametro("INSCRITOS")
            };
        }

        private void ClassificarListaSorteioSimples(int idUltimaLista)
        {
            ClassificarListaSorteio(idUltimaLista, "SIMPLES");
        }

        private void ClassificarListaSorteio(int idUltimaLista, string tipoOrdenacao)
        {

            List<CandidatoGrupo> candidatosLista = new List<CandidatoGrupo>();

            using (SQLiteCommand command = CreateCommand("SELECT CANDIDATO_LISTA.ID_LISTA, CANDIDATO.ID_CANDIDATO, CANDIDATO.CPF, CANDIDATO.NOME FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA"))
            {
                command.Parameters.Add("ID_LISTA", DbType.Int32);
                command.Parameters["ID_LISTA"].Value = idUltimaLista;

                using (SQLiteDataReader resultSet = command.ExecuteReader())
                {
                    while (resultSet.Read())
                    {
                        candidatosLista.Add(new CandidatoGrupo
                        {
                            IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_CANDIDATO")]),
                            Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                            Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")).ToUpper().TrimEnd()
                        });
                    }
                }
            }

            CandidatoGrupo[] candidatosOrdenados;

            candidatosOrdenados = candidatosLista
                .OrderBy(c => c.IdInscricao)
                .ToArray();

            CandidatoGrupo candidatoAnterior = null;
            int sequencia = 1;
            int classificacao = 1;

            SQLiteCommand updateCommand = CreateCommand(
                "UPDATE CANDIDATO_LISTA SET SEQUENCIA = @SEQUENCIA, CLASSIFICACAO = @CLASSIFICACAO WHERE ID_LISTA = @ID_LISTA AND ID_CANDIDATO = @ID_CANDIDATO",
                new SQLiteParameter("SEQUENCIA", -1) { DbType = DbType.Int32 },
                new SQLiteParameter("CLASSIFICACAO", -1) { DbType = DbType.Int32 },
                new SQLiteParameter("ID_LISTA", idUltimaLista) { DbType = DbType.Int32 },
                new SQLiteParameter("ID_CANDIDATO", -1) { DbType = DbType.Int32 }
            );
            updateCommand.Prepare();

            foreach (CandidatoGrupo candidato in candidatosOrdenados)
            {
                updateCommand.Parameters["SEQUENCIA"].Value = sequencia;
                updateCommand.Parameters["CLASSIFICACAO"].Value = classificacao;
                updateCommand.Parameters["ID_CANDIDATO"].Value = candidato.IdCandidato;
                updateCommand.ExecuteNonQuery();

                sequencia++;
                candidatoAnterior = candidato;
            }
        }

        public int ContagemCandidatos()
        {
            return ExecuteScalar("SELECT COUNT(*) FROM CANDIDATO");
        }

        private bool ConverterBooleano(object valor)
        {
            string valorComparacao = valor.ToString().ToUpper();

            if (listaBooleano.Contains(valorComparacao))
            {
                return valorComparacao == "SIM";
            }
            else
            {
                return Convert.ToBoolean(valor);
            }
        }

        private int ConverterInteiro(object valor)
        {
            string valorCompleto = valor.ToString();
            string inteiro = "";
            for (int ix = 0; ix < valorCompleto.Length; ix++)
            {
                if (valorCompleto[ix] >= '0' && valorCompleto[ix] <= '9')
                {
                    inteiro += valorCompleto[ix];
                }
            }

            return Int32.Parse(inteiro);
        }

        public void CopiarCandidatosArquivo(string faixa, IDataReader dataReader, Action<string> updateStatus, Action<int> updateProgress)
        {
            string inicio = "Iniciando importação ";
            updateStatus(inicio + faixa);

            /* Copia os candidatos da lista de importação. */

            updateStatus("Importando candidatos " + faixa);

            int idCandidato = 0;

            while (dataReader.Read())
            {
                idCandidato++;

                string cpf = AnonimizarCpf(dataReader[dataReader.GetOrdinal("CPF")]);
                string nome = dataReader.GetString(dataReader.GetOrdinal("NOME"));
                bool veiculoAdaptado = ConverterBooleano(dataReader[dataReader.GetOrdinal("ADAPTADO")]);
                bool veiculoHibrido = ConverterBooleano(dataReader[dataReader.GetOrdinal("HIBRIDO")]);
                bool veiculoComum = ConverterBooleano(dataReader[dataReader.GetOrdinal("COMUM")]);
                bool auxiliar = ConverterBooleano(dataReader[dataReader.GetOrdinal("CONDUTOR AUXILIAR")]);
                int inscricao = ConverterInteiro(dataReader[dataReader.GetOrdinal("INSCRICAO")]);

                ExecuteNonQuery($"INSERT INTO CANDIDATO (ID_CANDIDATO, CPF, NOME, VEICULO_ADAPTADO, VEICULO_HIBRIDO, VEICULO_COMUM, CONDUTOR_AUXILIAR, INSCRICAO) VALUES (@ID_CANDIDATO, @CPF, @NOME, @VADAPTADO, @VHIBRIDO, @VCOMUM, @AUXILIAR, @INSCRICAO)",
                    new SQLiteParameter("ID_CANDIDATO", idCandidato) { DbType = DbType.Int32 },
                    new SQLiteParameter("CPF", cpf) { DbType = DbType.String },
                    new SQLiteParameter("NOME", nome) { DbType = DbType.String },
                    new SQLiteParameter("VADAPTADO", veiculoAdaptado) { DbType = DbType.Boolean },
                    new SQLiteParameter("VHIBRIDO", veiculoHibrido) { DbType = DbType.Boolean },
                    new SQLiteParameter("VCOMUM", veiculoComum) { DbType = DbType.Boolean },
                    new SQLiteParameter("AUXILIAR", auxiliar) { DbType = DbType.Boolean },
                    new SQLiteParameter("INSCRICAO", inscricao) { DbType = DbType.Int32 }
                );
            }
        }

        public void CriarListasSorteioPorFaixa(string faixa, Action<string> updateStatus, Action<int> updateProgress, int listaAtual, int totalListas, int incrementoOrdem)
        {
            /* Gera as listas de sorteio por grupo e faixa de renda. */
            int idUltimaLista;

            updateStatus($"Gerando lista {listaAtual} de {totalListas}.");
            updateProgress((int)((++incrementoOrdem / (double)totalListas) * 100));

            idUltimaLista = CriarListaSorteioPorGrupoFaixa(faixa, "Condutor Auxiliar", listaAtual);
            bool veiculoComum = faixa.Contains("Comum");
            bool veiculoHibrido = faixa.Contains("Híbrido");
            bool veiculoAdaptado = faixa.Contains("Adaptado");
            ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA (ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE CONDUTOR_AUXILIAR = 1 AND ((@VADAPTADO = 1 AND VEICULO_ADAPTADO = @VADAPTADO) OR (@VHIBRIDO = 1 AND VEICULO_HIBRIDO = @VHIBRIDO) OR (@VCOMUM = 1 AND VEICULO_COMUM = @VCOMUM))",
                new SQLiteParameter("VADAPTADO", veiculoAdaptado) { DbType = DbType.Boolean },
                new SQLiteParameter("VHIBRIDO", veiculoHibrido) { DbType = DbType.Boolean },
                new SQLiteParameter("VCOMUM", veiculoComum) { DbType = DbType.Boolean }
            );
            ClassificarListaSorteioSimples(idUltimaLista);

            updateStatus($"Gerando lista {++listaAtual} de {totalListas}.");
            updateProgress((int)((++incrementoOrdem / (double)totalListas) * 100));

            idUltimaLista = CriarListaSorteioPorGrupoFaixa(faixa, "Cadastro Geral", listaAtual);
            ExecuteNonQuery($"INSERT INTO CANDIDATO_LISTA (ID_LISTA, ID_CANDIDATO) SELECT {idUltimaLista}, ID_CANDIDATO FROM CANDIDATO WHERE (@VADAPTADO = 1 AND VEICULO_ADAPTADO = @VADAPTADO) OR (@VHIBRIDO = 1 AND VEICULO_HIBRIDO = @VHIBRIDO) OR (@VCOMUM = 1 AND VEICULO_COMUM = @VCOMUM)",
                new SQLiteParameter("VADAPTADO", veiculoAdaptado) { DbType = DbType.Boolean },
                new SQLiteParameter("VHIBRIDO", veiculoHibrido) { DbType = DbType.Boolean },
                new SQLiteParameter("VCOMUM", veiculoComum) { DbType = DbType.Boolean }
            );
            ClassificarListaSorteioSimples(idUltimaLista);
        }

        private int CriarListaSorteioPorGrupoFaixa(string grupoFaixa, string nomeLista, int incremento)
        {
            bool candidatoGeral = nomeLista.Contains("Geral");
            bool condutorAuxiliar = nomeLista.Contains("Auxiliar");
            bool veiculoAdaptado = grupoFaixa.Contains("Adaptado");
            bool veiculoComum = grupoFaixa.Contains("Comum");
            bool veiculoHibrido = grupoFaixa.Contains("Híbrido");
            string nomeListaInsercao = String.Concat(nomeLista, " - ", grupoFaixa);

            ExecuteNonQuery(
                $"INSERT INTO LISTA (ID_LISTA, NOME, ORDEM_SORTEIO, QUANTIDADE, SORTEADA, PUBLICADA, CONDUTOR_AUXILIAR, CANDIDATO_GERAL, VEICULO_ADAPTADO, VEICULO_HIBRIDO, VEICULO_COMUM) VALUES(@ID_LISTA, @NOME_LISTA, @INCREMENTO_ORDEM, 1, 0, 0, @CAUXILIAR, @CGERAL, @VADAPTADO, @VHIBRIDO, @VCOMUM);",
                new SQLiteParameter("ID_LISTA", incremento) { DbType = DbType.Int32 },
                new SQLiteParameter("NOME_LISTA", nomeListaInsercao) { DbType = DbType.String },
                new SQLiteParameter("INCREMENTO_ORDEM", incremento) { DbType = DbType.Int32 },
                new SQLiteParameter("CAUXILIAR", condutorAuxiliar) { DbType = DbType.Boolean },
                new SQLiteParameter("CGERAL", candidatoGeral) { DbType = DbType.Boolean },
                new SQLiteParameter("VADAPTADO", veiculoAdaptado) { DbType = DbType.Boolean },
                new SQLiteParameter("VHIBRIDO", veiculoHibrido) { DbType = DbType.Boolean },
                new SQLiteParameter("VCOMUM", veiculoComum) { DbType = DbType.Boolean }
            );
            return incremento;
        }


        public static string DiretorioExportacaoCSV => $"{AppDomain.CurrentDomain.BaseDirectory}CSV";

        public void ExibirSorteado(CandidatoGrupo candidatoSorteado, int idLista, Action<string, bool> logText)
        {
            string nomeSorteado = string.Format("{0:0000}   {1}   {2} ({3})", candidatoSorteado.Sequencia, candidatoSorteado.Cpf, candidatoSorteado.Nome.ToUpper(), candidatoSorteado.IdInscricao.ToString());
            int indice = 0;
            int pos = nomeSorteado.Length;
            DateTime momento = DateTime.Now;
            DateTime momentoFinal = DateTime.Now.AddMilliseconds(5000);

            int posNome = candidatoSorteado.Nome.Length;
            string nomeDecifrar = candidatoSorteado.Nome;

            bool concluido = false;
            string complemento = "zmylxkwjviuhtgsfreqdpcobna";
            nomeDecifrar = nomeDecifrar.ToLower() + complemento;
            posNome = nomeDecifrar.Length;
            while (!concluido)
            {
                indice = 0;
                int trecho = 0;
                while (indice < posNome)
                {
                    trecho = posNome - indice - 1;
                    if (nomeDecifrar[indice] != ' ')
                    {
                        nomeDecifrar = nomeDecifrar.Substring(0, indice) + (char)((int)nomeDecifrar[indice] - 1) + nomeDecifrar.Substring(indice + 1, trecho);
                    }
                    indice++;
                }

                logText(nomeDecifrar, true);

                if (String.IsNullOrWhiteSpace(nomeDecifrar))
                {
                    nomeDecifrar = candidatoSorteado.Nome;
                }

                if (nomeDecifrar.TrimEnd() == candidatoSorteado.Nome)
                {
                    concluido = true;
                }

                momento = DateTime.Now;
                momentoFinal = DateTime.Now.AddMilliseconds(100);
                while (momento < momentoFinal)
                {
                    momento = DateTime.Now;
                }
                indice++;
            }

            logText(string.Format("{0}   {1}   ({2})", candidatoSorteado.Cpf, candidatoSorteado.Nome.ToUpper(), candidatoSorteado.IdInscricao.ToString()), true);

            momento = DateTime.Now;
            momentoFinal = DateTime.Now.AddMilliseconds(3000);
            while (momento < momentoFinal)
            {
                momento = DateTime.Now;
            }

            logText(nomeSorteado, false);

            ExecuteNonQuery($"UPDATE CANDIDATO_LISTA SET EXIBIDO = @EXIBIDO WHERE ID_CANDIDATO = @ID_CANDIDATO AND ID_LISTA = @ID_LISTA",
                new SQLiteParameter("EXIBIDO", true) { DbType = DbType.Boolean },
                new SQLiteParameter("ID_CANDIDATO", candidatoSorteado.IdCandidato) { DbType = DbType.Int32 },
                new SQLiteParameter("ID_LISTA", idLista) { DbType = DbType.Int32 }
            );
        }

        public void ExportarListas(Action<string> updateStatus, string caminho)
        {

            updateStatus("Iniciando exportação...");

            string directoryPath = DiretorioExportacaoCSV;
            if (String.IsNullOrWhiteSpace(caminho))
            {
                if (Directory.Exists(directoryPath))
                {
                    updateStatus("Excluindo arquivos anteriores.");
                    Directory.Delete(directoryPath, true);
                }
                Directory.CreateDirectory(directoryPath);
            }
            else
            {
                directoryPath = caminho;
            }

            string[] tabelas = new string[] { "CANDIDATO", "LISTA", "CANDIDATO_LISTA" };
            foreach (string tabela in tabelas)
            {
                WriteTable(directoryPath, tabela, updateStatus);
            }

            updateStatus("Finalizando exportação...");
        }


        private int ObterSemente(ref string fonteSemente)
        {
            int? semente = null;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(@"https://www.random.org/cgi-bin/randbyte?nbytes=4&format=h").Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string content = response.Content.ReadAsStringAsync().Result;
                        semente = Convert.ToInt32(content.Replace(" ", ""), 16);
                        fonteSemente = "RANDOM.ORG";
                    }
                }
            }
            catch { }

            if (semente == null)
            {
                fonteSemente = "SISTEMA";
                return (int)DateTime.Now.Ticks;
            }
            else
            {
                return (int)semente;
            }
        }

        public Lista SortearCandidatos(Action<string> updateStatus, Action<int> updateProgress, Action<string, bool> logText, int? sementePersonalizada = null)
        {
            Lista proximaLista = CarregarProximaLista();
            if (proximaLista == null)
            {
                throw new Exception("Não existem listas disponíveis para sorteio.");
            }
            double quantidadeAtual = 0;
            double quantidadeTotal = Math.Min(proximaLista.Quantidade, (int)proximaLista.CandidatosDisponiveis);

            int semente = 0;
            System.Data.SqlTypes.SqlInt32 sementeConsultada = new System.Data.SqlTypes.SqlInt32();
            string querySementeLista = "SELECT SEMENTE_SORTEIO FROM LISTA WHERE ID_LISTA = @ID_LISTA";
            SQLiteCommand commandSementeLista = CreateCommand(querySementeLista);
            commandSementeLista.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);

            using (SQLiteDataReader resultadoSemente = commandSementeLista.ExecuteReader())
            {
                if (resultadoSemente.Read())
                {
                    if (!resultadoSemente.IsDBNull(resultadoSemente.GetOrdinal("SEMENTE_SORTEIO")))
                    {
                        sementeConsultada = Convert.ToInt32(resultadoSemente[resultadoSemente.GetOrdinal("SEMENTE_SORTEIO")]);
                    }
                }
            }

            if (sementeConsultada.IsNull)
            {
                string fonteSemente = "PERSONALIZADA";
                semente = (sementePersonalizada == null) ? ObterSemente(ref fonteSemente) : (int)sementePersonalizada;
                ExecuteNonQuery(
                    "UPDATE LISTA SET SEMENTE_SORTEIO = @SEMENTE_SORTEIO, FONTE_SEMENTE = @FONTE_SEMENTE WHERE ID_LISTA = @ID_LISTA",
                    new SQLiteParameter("SEMENTE_SORTEIO", semente) { DbType = DbType.Int32 },
                    new SQLiteParameter("FONTE_SEMENTE", fonteSemente) { DbType = DbType.String },
                    new SQLiteParameter("ID_LISTA", proximaLista.IdLista) { DbType = DbType.Int32 }
                );
            }
            else
            {
                semente = sementeConsultada.Value;
            }

            Random random = new Random(semente);

            string queryGrupoSorteio = @"
                SELECT CANDIDATO_LISTA.CLASSIFICACAO AS CLASSIFICACAO, COUNT(*) AS QUANTIDADE
                FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.DATA_CONTEMPLACAO IS NULL AND CANDIDATO.CONTEMPLADO = 0
                GROUP BY CANDIDATO_LISTA.CLASSIFICACAO
                ORDER BY CANDIDATO_LISTA.CLASSIFICACAO
            ";
            SQLiteCommand commandGrupoSorteio = CreateCommand(queryGrupoSorteio);
            commandGrupoSorteio.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);
            commandGrupoSorteio.Prepare();

            string queryCandidatosGrupo = @"
                SELECT CANDIDATO_LISTA.SEQUENCIA, CANDIDATO.ID_CANDIDATO, CANDIDATO.CPF, CANDIDATO.NOME, CANDIDATO.INSCRICAO
                FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.ID_LISTA = @ID_LISTA AND CANDIDATO_LISTA.DATA_CONTEMPLACAO IS NULL AND CANDIDATO.CONTEMPLADO = 0 AND CANDIDATO_LISTA.CLASSIFICACAO = @CLASSIFICACAO
                ORDER BY CANDIDATO_LISTA.SEQUENCIA
            ";
            SQLiteCommand commandCandidatosGrupo = CreateCommand(queryCandidatosGrupo);
            commandCandidatosGrupo.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);
            commandCandidatosGrupo.Parameters.AddWithValue("CLASSIFICACAO", -1);
            commandCandidatosGrupo.Prepare();

            GrupoSorteio grupoSorteio = null;
            StringBuilder lista = new StringBuilder();

            string queryQuantidadeExibidos = "SELECT COUNT(*) FROM CANDIDATO_LISTA WHERE ID_LISTA = @ID_LISTA AND EXIBIDO = 1";
            SQLiteCommand commandQtdExibidos = CreateCommand(queryQuantidadeExibidos);
            commandQtdExibidos.Parameters.AddWithValue("ID_LISTA", proximaLista.IdLista);

            int qtdExibidos = ExecuteScalar(queryQuantidadeExibidos, new SQLiteParameter("ID_LISTA", proximaLista.IdLista) { DbType = DbType.Int32 });

            for (int i = (qtdExibidos + 1); i <= proximaLista.Quantidade; i++)
            {
                if (grupoSorteio == null || grupoSorteio.Quantidade < 1)
                {
                    using (SQLiteDataReader resultSet = commandGrupoSorteio.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (resultSet.Read())
                        {
                            grupoSorteio = new GrupoSorteio
                            {
                                Classificacao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("CLASSIFICACAO")]),
                                Quantidade = Convert.ToInt32(resultSet[resultSet.GetOrdinal("QUANTIDADE")])
                            };
                        }
                        else
                        {
                            return proximaLista;
                        }
                    }
                    if (grupoSorteio != null)
                    {
                        commandCandidatosGrupo.Parameters["CLASSIFICACAO"].Value = grupoSorteio.Classificacao;
                        using (SQLiteDataReader resultSet = commandCandidatosGrupo.ExecuteReader())
                        {
                            while (resultSet.Read())
                            {
                                CandidatoGrupo candidato = new CandidatoGrupo
                                {
                                    Sequencia = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEQUENCIA")]),
                                    IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_CANDIDATO")]),
                                    Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                                    Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                                    IdInscricao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("INSCRICAO")])
                                };
                                grupoSorteio.Candidatos.Add(candidato.Sequencia, candidato);
                            }
                        }
                    }
                }

                if (grupoSorteio == null)
                {
                    return proximaLista;
                }

                int indiceSorteado = (grupoSorteio.Quantidade == 1) ? 0 : random.Next(0, grupoSorteio.Quantidade);
                CandidatoGrupo candidatoSorteado = grupoSorteio.Candidatos.Skip(indiceSorteado).Take(1).First().Value;
                candidatoSorteado.Nome = candidatoSorteado.Nome.ToUpper();
                grupoSorteio.Candidatos.Remove(candidatoSorteado.Sequencia);
                bool contemplado = true;

                ExecuteNonQuery("UPDATE CANDIDATO SET CONTEMPLADO = @CONTEMPLADO WHERE ID_CANDIDATO = @ID_CANDIDATO",
                    new SQLiteParameter("ID_CANDIDATO", candidatoSorteado.IdCandidato) { DbType = DbType.Int32 },
                    new SQLiteParameter("CONTEMPLADO", contemplado) { DbType = DbType.Boolean });

                ExecuteNonQuery(
                    @"
                        UPDATE CANDIDATO_LISTA
                        SET SEQUENCIA_CONTEMPLACAO = @SEQUENCIA_CONTEMPLACAO, DATA_CONTEMPLACAO = @DATA_CONTEMPLACAO
                        WHERE ID_CANDIDATO = @ID_CANDIDATO AND ID_LISTA = @ID_LISTA
                    ",
                    new SQLiteParameter("SEQUENCIA_CONTEMPLACAO", i) { DbType = DbType.Int32 },
                    new SQLiteParameter("DATA_CONTEMPLACAO", DateTime.Now) { DbType = DbType.DateTime },
                    new SQLiteParameter("ID_CANDIDATO", candidatoSorteado.IdCandidato) { DbType = DbType.Int32 },
                    new SQLiteParameter("ID_LISTA", proximaLista.IdLista) { DbType = DbType.Int32 }
                );

                grupoSorteio.Quantidade--;
                quantidadeAtual++;

                updateProgress((int)((quantidadeAtual / quantidadeTotal) * 100));

            }
            return proximaLista;
        }

        public Lista SortearProximaLista(Action<string> updateStatus, Action<int> updateProgress, Action<string, bool> logText, int? sementePersonalizada = null)
        {
            Lista lista = null;
            string queryCandidatosNaoExibidos = @"
                SELECT CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO, CANDIDATO.ID_CANDIDATO, CANDIDATO.CPF, CANDIDATO.NOME, CANDIDATO.INSCRICAO, CANDIDATO_LISTA.ID_LISTA, CANDIDATO_LISTA.EXIBIDO
                FROM CANDIDATO_LISTA INNER JOIN CANDIDATO ON CANDIDATO_LISTA.ID_CANDIDATO = CANDIDATO.ID_CANDIDATO
                WHERE CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO IS NOT NULL AND CANDIDATO.CONTEMPLADO = 1 AND CANDIDATO_LISTA.EXIBIDO = 0
                ORDER BY CANDIDATO_LISTA.SEQUENCIA_CONTEMPLACAO
            ";
            SQLiteCommand commandCandidatosNaoExibidos = CreateCommand(queryCandidatosNaoExibidos);
            commandCandidatosNaoExibidos.Prepare();

            using (SQLiteDataReader resultSet = commandCandidatosNaoExibidos.ExecuteReader())
            {
                if (resultSet.HasRows)
                {
                    resultSet.Read();
                    int idLista = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_LISTA")]);
                    string queryNomeLista = @"
                        SELECT NOME, ORDEM_SORTEIO
                        FROM LISTA
                        WHERE ID_LISTA = @ID_LISTA
                    ";
                    SQLiteCommand commandNomeLista = CreateCommand(queryNomeLista);
                    commandNomeLista.Parameters.AddWithValue("ID_LISTA", idLista);
                    commandNomeLista.Prepare();
                    SQLiteDataReader resultNomeLista = commandNomeLista.ExecuteReader();
                    resultNomeLista.Read();
                    string nomeLista = resultNomeLista.GetString(resultNomeLista.GetOrdinal("NOME"));
                    int ordemSorteioLista = Convert.ToInt32(resultNomeLista[resultNomeLista.GetOrdinal("ORDEM_SORTEIO")]);
                    int qtdCandidatos = CandidatosDisponiveisLista(idLista);
                    updateStatus(String.Concat("Sorteando lista ", ordemSorteioLista.ToString("00"), " - ", nomeLista, " - ", qtdCandidatos, " candidatos"));
                    CandidatoGrupo candidatoSorteado = new CandidatoGrupo
                    {
                        Sequencia = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEQUENCIA_CONTEMPLACAO")]),
                        IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_CANDIDATO")]),
                        Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                        Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                        IdInscricao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("INSCRICAO")])
                    };
                    ExibirSorteado(candidatoSorteado, idLista, logText);
                    if (nomeLista.Contains("SUPLENTE") || sementePersonalizada != null)
                    {
                        while (resultSet.Read())
                        {
                            candidatoSorteado = new CandidatoGrupo
                            {
                                Sequencia = Convert.ToInt32(resultSet[resultSet.GetOrdinal("SEQUENCIA_CONTEMPLACAO")]),
                                IdCandidato = Convert.ToInt32(resultSet[resultSet.GetOrdinal("ID_CANDIDATO")]),
                                Cpf = resultSet.GetString(resultSet.GetOrdinal("CPF")),
                                Nome = resultSet.GetString(resultSet.GetOrdinal("NOME")),
                                IdInscricao = Convert.ToInt32(resultSet[resultSet.GetOrdinal("INSCRICAO")])
                            };
                            ExibirSorteado(candidatoSorteado, idLista, logText);
                        }
                        lista = new Lista { IdLista = idLista, Nome = nomeLista, OrdemSorteio = ordemSorteioLista };
                        AtualizarListaSorteada(lista, updateStatus);
                    }
                    else
                    {
                        if (resultSet.Read() == false)
                        {
                            lista = new Lista { IdLista = idLista, Nome = nomeLista, OrdemSorteio = ordemSorteioLista };
                            AtualizarListaSorteada(lista, updateStatus);
                        }
                    }
                    resultNomeLista.Close();
                }
                else
                {
                    lista = SortearCandidatos(updateStatus, updateProgress, logText, sementePersonalizada);
                    if (lista != null)
                    {
                        if (lista.CandidatosDisponiveis > 0 && lista.Quantidade > 0)
                        {
                            lista = SortearProximaLista(updateStatus, updateProgress, logText, sementePersonalizada);
                            if (lista != null)
                            {
                                int qtdCandidatos = CandidatosDisponiveisLista(lista.IdLista);
                                updateStatus(String.Concat("Sorteio Lista ", lista.OrdemSorteio.ToString("00"), " - ", lista.Nome, " - ", qtdCandidatos, " candidatos"));
                            }
                        }
                        else
                        {
                            AtualizarListaSorteada(lista, updateStatus);
                        }
                    }
                }
                resultSet.Close();
            }
            return lista;
        }

        public string ValidarCabecalho(string linhaCabecalho)
        {
            StringBuilder listagem = new StringBuilder();
            string[] termosCabecalho = { "INSCRICAO", "CPF", "NOME", "ADAPTADO", "HIBRIDO", "COMUM", "CONDUTOR AUXILIAR" };

            for(int ix=0; ix < termosCabecalho.Length; ix++)
            {
                if (!linhaCabecalho.Contains(termosCabecalho[ix]))
                {
                    listagem.AppendLine(String.Concat(" - ", termosCabecalho[ix]));
                }
            }

            if (listagem.Length > 0)
            {
                listagem.Insert(0, "Cabeçalho do arquivo de entrada inválido.\n\nLista de colunas não encontradas no cabeçalho do arquivo de Candidatos Inscritos:\n\n");
            }

            return listagem.ToString();
        }

        private void WriteTable(string directoryPath, string tableName, Action<string> updateStatus)
        {
            int count = 0;
            int total = ExecuteScalar($"SELECT COUNT(*) FROM {tableName}");

            using (StreamWriter writter = new StreamWriter($"{directoryPath}/{tableName}.CSV"))
            {
                using (SQLiteCommand command = CreateCommand($"SELECT * FROM {tableName}"))
                {
                    using (SQLiteDataReader dataReader = command.ExecuteReader())
                    {
                        IEnumerable<int> fieldRange = Enumerable.Range(0, dataReader.FieldCount);
                        CsvWriter.WriteRow(writter, fieldRange.Select(i => dataReader.GetName(i).ToLower()).ToArray());
                        while (dataReader.Read())
                        {
                            updateStatus($"Exportando tabela \"{tableName}\" - linha {++count} de {total}.");
                            CsvWriter.WriteRow(
                                writter,
                                fieldRange.Select(i => dataReader.GetValue(i))
                                    .Select(i =>
                                    {
                                        if (i is bool)
                                        {
                                            return ((bool)i) ? "1" : "0";
                                        }
                                        else
                                        {
                                            return i.ToString();
                                        }
                                    })
                                    .ToArray()
                            );
                        }
                    }
                };
            }
        }
    }
}
