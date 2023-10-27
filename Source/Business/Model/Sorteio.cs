using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Maistaxi.Business.Model {
    public class Sorteio : IDataErrorInfo, INotifyPropertyChanged {

        private string nome;
        private string statusSorteio;
        private string inscritos;
        private ICollection<Lista> listas;
        private Lista proximaLista;

        public Sorteio() {
            listas = new List<Lista>();
        }

        public string Nome {
            get { return nome; }
            set { SetField(ref nome, value); }
        }

        public string StatusSorteio {
            get { return statusSorteio; }
            set { SetField(ref statusSorteio, value); }
        }

        public string Inscritos
        {
            get { return inscritos; }
            set { SetField(ref inscritos, value); }
        }

        public ICollection<Lista> Listas {
            get { return listas; }
            set {
                value.ToList().ForEach(l => l.Sorteio = this);
                SetField(ref listas, value);
                NotifyPropertyChanged("TotalVagasTitulares");
                NotifyPropertyChanged("TotalVagasReserva");
                NotifyPropertyChanged("TotalVagas");
            }
        }

         public Lista ProximaLista {
            get { return proximaLista; }
            set {
                SetField(ref proximaLista, value);
            }
        }

        public int? TotalVagasTitulares => listas.Where(l => !l.Nome.ToUpper().Contains("SUPLENTES")).Sum(l => l.Quantidade);
        public int? TotalVagasReserva => listas.Where(l => l.Nome.ToUpper().Contains("SUPLENTES")).Sum(l => l.Quantidade);
        public int? TotalVagas => listas.Sum(l => l.Quantidade);

        /* INotifyPropertyChanged */

        #region INotifyPropertyChanged

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) {
                return false;
            }
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public void NotifyPropertyChanged(string property) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /* IDataErrorInfo */

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error { get { return null; } }

        string IDataErrorInfo.this[string columnName] { get {
            if (columnName == "Nome" && string.IsNullOrWhiteSpace(Nome)) return "Nome inválido!";
            if (columnName == "Inscritos" && string.IsNullOrWhiteSpace(Inscritos)) return "Arquivo de inscritos inválido!";
            return null;
        }}

        public bool IsValid { get {
            IDataErrorInfo errorInfo = this as IDataErrorInfo;
            return errorInfo["Nome"] == null
                && errorInfo["Inscritos"] == null;
        }}

        #endregion
    }
}
