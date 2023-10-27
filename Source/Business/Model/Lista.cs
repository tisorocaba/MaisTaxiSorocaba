using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Maistaxi.Business.Model {
    public class Lista : IDataErrorInfo {

        public Sorteio Sorteio { get; set; }

        public int IdLista { get; set; }
        public int OrdemSorteio { get; set; }
        public string Nome { get; set; }
        public string Candidato { get; set; }
        public bool Sorteada { get; set; }
        public bool Publicada { get; set; }
        public bool CandidatoGeral { get; set; }
        public bool CondutorAuxiliar { get; set; }
        public String FonteSemente { get; set; }
        public int? SementeSorteio { get; set; }
        public bool VeiculoAdaptado { get; set; }
        public bool VeiculoComum { get; set; }
        public bool VeiculoHibrido { get; set; }

        private int quantidade;
        public int Quantidade {
            get { return quantidade; }
            set {
                quantidade = value;
                QuantidadeString = quantidade.ToString();
            }
        }

        private string quantidadeString;
        public string QuantidadeString
        {
            get { return quantidadeString; }
            set
            {
                quantidadeString = value;
                if (!String.IsNullOrWhiteSpace(QuantidadeString) && Regex.IsMatch(QuantidadeString, @"^\d+$")) {
                    quantidade = int.Parse(value);
                    if (Sorteio != null) {
                        Sorteio.NotifyPropertyChanged("TotalVagasTitulares");
                        Sorteio.NotifyPropertyChanged("TotalVagasReserva");
                        Sorteio.NotifyPropertyChanged("TotalVagas");
                    }
                }
            }
        }

        public int? CandidatosDisponiveis { get; set; }

        /* Exibição */

        public string VagasText => $"{Quantidade} autorizações de táxi.";
        public string CandidatosText => $"{CandidatosDisponiveis} motoristas.";
        public string NomeFormatado => String.Format("{0:00} - {1}", OrdemSorteio, Nome);
        public string CandidatoSorteado => String.Format("{0}", Candidato);

        /* IDataErrorInfo */

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error { get { return null; } }

        string IDataErrorInfo.this[string columnName] { get {
            if (columnName == "QuantidadeString") {
                if (String.IsNullOrWhiteSpace(QuantidadeString) || !Regex.IsMatch(QuantidadeString, @"^\d+$")) {
                    return "Quantidade inválida.";
                }
            }
            return null;
        }}

        public bool IsValid { get {
            IDataErrorInfo errorInfo = this as IDataErrorInfo;
            return errorInfo["QuantidadeString"] == null;
        }}

        #endregion
    }
}
