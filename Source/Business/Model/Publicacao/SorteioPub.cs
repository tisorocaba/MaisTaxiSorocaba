using System.Collections.Generic;

namespace Maistaxi.Business.Model.Publicacao {
    public class SorteioPub {
        public int? Codigo { get; set; }
        public string Nome { get; set; }
        public virtual ICollection<ListaPub> Listas { get; set; }
    }
}
