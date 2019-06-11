using Objetos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsServiceOriginal
{
    internal class Loader
    {
        public void StartProcess()
        {
            var ListRegraSADT = new List<Regra>();
            var ListRegraInternacao = new List<Regra>();

            try
            {
                {   // Iniciar LightInjection Scope
                    // ServiceLocator de Repositorios
                    // Objeto de configuração de e-mail

                    while (true)
                    {
                        var faturas = Mocks.Faturas(qtdFaturas: 3000);

                        if (faturas.Any())
                        {
                            var lastUpdate = DateTime.MinValue;
                            var ListTodasRegras = new List<Regra>();
                            ListTodasRegras.AddRange(ListRegraSADT);
                            ListTodasRegras.AddRange(ListRegraInternacao);
                            var dataInsercao = ListTodasRegras.Count > 0 ? ListTodasRegras.Max(o => o.DataInsercao) : DateTime.MinValue;
                            var dataAlteracao = ListTodasRegras.Count > 0 ? ListTodasRegras.Max(o => o.DataAlteracao) : DateTime.MinValue;

                            lastUpdate = dataInsercao;
                            if (dataAlteracao > dataInsercao)
                                lastUpdate = dataAlteracao;

                            // Utiliza lastUpdate para verificar se ocorreu atualização das listas
                            if (ListTodasRegras.Count <= 0)
                                Mocks.Regras.CarregarListaDeRegras(
                                    listRegraSADT: ListRegraSADT,
                                    listRegraInternacao: ListRegraInternacao);

                            SemaphoreSlim semaphore = new SemaphoreSlim(1);

                            var dadosParaValidacao = new DadosParaValidacao(
                                listRegraSADT: ListRegraSADT,
                                listRegraInternacao: ListRegraInternacao);

                            Task task = new Task(delegate
                            {
                                EfetuarValidacao(faturas, dadosParaValidacao, semaphore);
                            });
                            task.Start();
                        }
                        Thread.Sleep(15 * 1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Mocks.ServiceRepository.Update("Erro - " + ex.Message);
            }
        }

        private static void EfetuarValidacao(List<Fatura> faturas, DadosParaValidacao dadosParaValidacao, SemaphoreSlim semaphore)
        {
            {   // Inicializa escopo do LightInjector
                try
                {
                    semaphore.Wait();
                    Mocks.ServiceRepository.Update("Iniciando processo - " + DateTime.Now);
                    semaphore.Release();

                    Validar(faturas, dadosParaValidacao, semaphore);

                    semaphore.Wait();
                    Mocks.ServiceRepository.Update("Finalizando processo - " + DateTime.Now);
                    semaphore.Release();
                }
                catch (Exception ex)
                {
                    Mocks.ServiceRepository.Update("Erro - " + ex.Message);
                }
            }
        }

        private static void Validar(List<Fatura> faturas, DadosParaValidacao dadosParaValidacao, SemaphoreSlim semaphore)
        {
            List<Task> tasks = new List<Task>();
            List<Validacao> validacoes = new List<Validacao>();

            foreach (var fatura in faturas)
            {
                var task = new Task(delegate
                {
                    var validacao = ValidarFatura(fatura, dadosParaValidacao);
                    validacoes.Add(validacao);
                });
                tasks.Add(task);
                task.Start();
            }
            Task.WaitAll(tasks.ToArray());

            foreach (var validacao in validacoes)
            {
                semaphore.Wait();

                Mocks.FaturaRepository.UpdateStatusWithLock(validacao.Fatura);

                semaphore.Release();
            }
        }

        public static Validacao ValidarFatura(Fatura fatura, DadosParaValidacao dadosParaValidacao)
        {
            try
            {
                fatura.CNPJ = string.Concat(fatura.CNPJ.Where(char.IsDigit));
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
    }
}