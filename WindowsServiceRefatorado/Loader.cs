using Objetos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsServiceRefatorado
{
    internal class Loader
    {
        // Tasks de Processamento de faturas
        private List<Task> tasks = new List<Task>();

        // Cancellation Token
        private CancellationToken _cancellationToken;

        public Loader(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void StartProcess()
        {
            var listRegraSADT = new List<Regra>();
            var listRegraInternacao = new List<Regra>();

            try
            {
                {   // Iniciar LightInjection Scope
                    // ServiceLocator de Repositorios
                    // Objeto de configuração de e-mail

                    var iteration = 0;

                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        var faturas = Mocks.FaturaEmpty;

                        // Numero maximo de iterações na PoC
                        if (iteration < Config.MAX_WHILE_ITERATION)
                        {
                            faturas = Mocks.Faturas(qtdFaturas: Config.NUMBER_OF_INVOICE);

                            iteration++;
                        }
                        else
                            Program.OnStop();

                        VerificarAtualizacaoDeRegras(
                            listRegraSADT: listRegraSADT,
                            listRegraInternacao: listRegraInternacao);

                        if (faturas.Any())
                        {
                            var dadosParaValidacao = new DadosParaValidacao(
                                listRegraSADT: listRegraSADT,
                                listRegraInternacao: listRegraInternacao);

                            ProcessarFaturas(faturas, dadosParaValidacao);
                        }

                        if (tasks.Count > 0)
                            WaitTasks(tasks: tasks);
                        else
                            Thread.Sleep(millisecondsTimeout: Config.POOLING_INTERVAL);
                    }
                }
            }
            catch (Exception ex)
            {
                Mocks.ServiceRepository.Update("Erro - " + ex.Message);
            }
        }

        private void ProcessarFaturas(List<Fatura> faturas, DadosParaValidacao dadosParaValidacao)
        {
            for (int i = 0; i < faturas.Count; i += Config.MAX_FATURA_PER_TASK)
            {
                List<Fatura> faturasShard;

                if (faturas.Count < Config.MAX_FATURA_PER_TASK)
                {
                    faturasShard = faturas;
                }
                else if (i < faturas.Count && (faturas.Count - i) >= Config.MAX_FATURA_PER_TASK)
                {
                    faturasShard = faturas.GetRange(
                        index: i,
                        count: Config.MAX_FATURA_PER_TASK);
                }
                else
                {
                    var count = faturas.Count - i;

                    faturasShard = faturas.GetRange(
                        index: faturas.Count - count,
                        count: count);
                }

                tasks.Add(item: Task.Run(
                    action: () => EfetuarValidacao(
                        faturas: faturasShard,
                        dadosParaValidacao: dadosParaValidacao)));
            }
        }

        private static void EfetuarValidacao(List<Fatura> faturas, DadosParaValidacao dadosParaValidacao)
        {
            {   // Inicializa escopo do LightInjector
                try
                {
                    Mocks.ServiceRepository.Update("Iniciando processo - " + DateTime.Now);

                    Validar(faturas, dadosParaValidacao);

                    Mocks.ServiceRepository.Update("Finalizando processo - " + DateTime.Now);
                }
                catch (Exception ex)
                {
                    Mocks.ServiceRepository.Update("Erro - " + ex.Message);
                }
            }
        }

        private static void Validar(List<Fatura> faturas, DadosParaValidacao dadosParaValidacao)
        {
            List<Task> tasks = new List<Task>();
            List<Validacao> validacoes = new List<Validacao>();

            foreach (var fatura in faturas)
            {
                tasks.Add(
                    item: Task.Run(
                        action: () =>
                        {
                            var validacao = ValidarFatura(
                                fatura: fatura,
                                dadosParaValidacao: dadosParaValidacao);

                            validacoes.Add(item: validacao);
                        }));
            }

            WaitTasks(tasks: tasks);

            foreach (var validacao in validacoes)
            {
                Mocks.FaturaRepository.UpdateStatus(validacao.Fatura);
            }
        }

        public static Validacao ValidarFatura(Fatura fatura, DadosParaValidacao dadosParaValidacao)
        {
            try
            {
                unsafe
                {
                    var cnpj = stackalloc char[Config.CNPJ_MAX_SIZE];
                    var i = 0;

                    foreach (var c in fatura.CNPJ)
                    {
                        if (char.IsDigit(c))
                        {
                            cnpj[i] = c;
                            i++;
                        }
                    }

                    fatura.CNPJ = new string(value: cnpj);
                }

                var nomeArquivo = fatura.NomeArquivo;
                fatura.NomeArquivo = "Nome_Do_Arquivo";

                return Mocks.Validacao(fatura);
            }
            catch (Exception ex)
            {
                var validacao = new Validacao
                {
                    Data = DateTime.Now,
                    Fatura = fatura,
                    FaturaId = fatura.Id,
                    Guia = string.Empty,
                    Lote = string.Empty
                };

                var ListValidacaoErroPrograma = new List<ValidacaoErro>();

                ListValidacaoErroPrograma.Add(new ValidacaoErro
                {
                    Descricao = "Ocorreu um erro",
                    Detalhes = ex.Message
                });

                validacao.ListValidacaoErro.AddRange(ListValidacaoErroPrograma);

                return validacao;
            }
        }

        private void VerificarAtualizacaoDeRegras(List<Regra> listRegraSADT, List<Regra> listRegraInternacao)
        {
            if (listRegraSADT.Count == 0 || listRegraInternacao.Count == 0)
                Mocks.Regras.CarregarListaDeRegras(
                    listRegraInternacao: listRegraSADT,
                    listRegraSADT: listRegraSADT);
        }

        private static void WaitTasks(List<Task> tasks)
        {
            try
            {
                Task.WaitAll(tasks: tasks.ToArray());
            }
            catch (Exception ex)
            {
                Mocks.ServiceRepository.Update("Erro - " + ex.Message);
            }
            finally
            {
                tasks.Clear();
            }
        }
    }
}