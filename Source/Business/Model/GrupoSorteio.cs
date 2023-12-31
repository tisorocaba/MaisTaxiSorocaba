﻿using System.Collections.Generic;

namespace Maistaxi.Business.Model {
    public class GrupoSorteio {

        public GrupoSorteio() {
            Candidatos = new SortedList<int, CandidatoGrupo>();
        }

        public int Classificacao { get; set; }
        public int Quantidade { get; set; }

        public SortedList<int, CandidatoGrupo> Candidatos { get; set; }
    }
}
