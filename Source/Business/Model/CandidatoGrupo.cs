namespace Maistaxi.Business.Model {
    public class CandidatoGrupo {
        public int? Classificacao { get; set; }
        public int Sequencia { get; set; }
        public string Cpf { get; set; }
        public int IdCandidato { get; set; }
        public string Nome { get; set; }

        public bool CondutorAuxiliar { get; set; }
        public bool VeiculoAdaptado { get; set; }
        public bool VeiculoComum { get; set; }
        public bool VeiculoHibrido { get; set; }
        public int IdInscricao { get; set; }
    }
}
