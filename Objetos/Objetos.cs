using System;
using System.Collections.Generic;

namespace Objetos
{
    public class Fatura
    {
        public long Id { get; set; }
        public string Prestador { get; set; }
        public string CodPrestador { get; set; }
        public string CNPJ { get; set; } = "12.323.234/4920-12";
        public string Numero { get; set; } = "5345-ar";
        public decimal Valor { get; set; }
        public string NomeArquivo { get; set; }
        public DateTime DataInsercao { get; set; }
        public DateTime DataAlteracao { get; set; }
        public DateTime DataPosProcessamento { get; set; }
        public long? PrestadorProgRecid { get; set; }
    }

    public class Validacao
    {
        public Validacao()
        {
            ListValidacaoErro = new List<ValidacaoErro>();
        }

        public long Id { get; set; }
        public long FaturaId { get; set; }
        public Fatura Fatura { get; set; }
        public string Lote { get; set; }
        public string Guia { get; set; }
        public DateTime Data { get; set; }
        public List<ValidacaoErro> ListValidacaoErro { get; set; }
    }

    public class ValidacaoErro
    {
        public long Id { get; set; }
        public string Descricao { get; set; }
        public string Detalhes { get; set; }
        public long? RegraId { get; set; }
        public long faturaId { get; set; }
        public Regra Regra { get; set; }
        public long ValidacaoId { get; set; }
    }

    public class Regra
    {
        public Regra()
        {
            ListaRegraComparacao = new List<RegraComparacao>();
        }

        public long Id { get; set; }
        public List<RegraComparacao> ListaRegraComparacao { get; set; }
        public string Obrigatorio { get; set; }
        public DateTime DataFim { get; set; }
        public DateTime DataInsercao { get; set; }
        public DateTime DataAlteracao { get; set; }
        public String UsuarioInsercao { get; set; }
    }

    public class RegraComparacao
    {
        public long Id { get; set; }
        public long ValorBuscaId { get; set; }
    }

    public class DadosParaValidacao
    {
        public List<Regra> ListRegraSADT { get; set; }
        public List<Regra> ListRegraInternacao { get; set; }
        public DadosParaValidacao(List<Regra> listRegraSADT, List<Regra> listRegraInternacao)
        {
            ListRegraSADT = listRegraSADT;
            ListRegraInternacao = listRegraInternacao;
        }
    }
}