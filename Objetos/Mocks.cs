using System;
using System.Collections.Generic;
using System.Threading;

namespace Objetos
{
    public static class Mocks
    {
        public static object lockObject = new object();
        private static Random ids = new Random();

        public static List<Fatura> Faturas(int qtdFaturas = 10)
        {
            var faturas = new List<Fatura>();

            for (int i = 0; i < qtdFaturas; i++)
            {
                var fatura = new Fatura()
                {
                    Id = ids.Next(1, 9999),
                    CNPJ = "12.546.234/0001-45"
                };

                faturas.Add(item: fatura);
            }

            return faturas;
        }

        public static Validacao Validacao(Fatura fatura)
        {
            var validacao = new Validacao()
            {
                Fatura = fatura,
                FaturaId = fatura.Id
            };

            Thread.Sleep(800);

            Console.WriteLine($"Validação da fatura {fatura.Id} concluída.");

            return validacao;
        }

        public static class FaturaRepository
        {
            public static void UpdateStatusWithLock(Fatura fatura)
            {
                lock (lockObject)
                {
                    // Simula um processamento e acesso a banco de dados

                    var time = fatura.CNPJ.Length;

                    Thread.Sleep(time * 50);
                }

                Console.WriteLine($"Atualização da fatura {fatura.Id} concluída.");
            }
        }

        public static class ServiceRepository
        {
            public static void Update(string info)
            {
                // Simula acesso ao banco de dados
                Thread.Sleep(300);

                Console.WriteLine($"Atualização de status de serviço concluída. {info}");
            }
        }

        public static class Regras
        {
            public static void CarregarListaDeRegras(List<Regra> listRegraSADT, List<Regra> listRegraInternacao)
            {
                for (int i = 0; i < 300; i++)
                {
                    listRegraSADT.Add(item: new Regra());
                    listRegraInternacao.Add(item: new Regra());
                }
            }
        }
    }
}