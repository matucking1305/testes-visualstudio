﻿using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace PluginsTreinamento
{
    public class WFCriarCalendarioAluno : CodeActivity
    {
        #region Parametros
        // recebe o usuário do contexto
        [Input("Usuario")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> usuarioEntrada { get; set; }

        // recebe o contexto
        [Input("AlunoXCursoDisponivel")]
        [ReferenceTarget("curso_alunoxcursodisponvel")]
        public InArgument<EntityReference> RegistroContexto { get; set; }

        [Output("Saida")]
        public OutArgument<string> saida { get; set; }
        #endregion
        protected override void Execute(CodeActivityContext executionContext)
        {       
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService trace = executionContext.GetExtension<ITracingService>();

            Guid guidAlunoXCurso = context.PrimaryEntityId;

            trace.Trace("guidAlunoXCurso: " + guidAlunoXCurso);

            Entity entityAlunoXCursoDisponivel = service.Retrieve("curso_alunoxcursodisponvel", guidAlunoXCurso,
                new ColumnSet("curso_cursoselecionado", "dio_periodo", "dio_datadeinicio"));

            Guid guidCurso = Guid.Empty;

  
            var PeriodoCurso = String.Empty;

            DateTime dataInicio = new DateTime();

            if (entityAlunoXCursoDisponivel != null)
            {
                guidCurso = ((EntityReference)entityAlunoXCursoDisponivel.Attributes["curso_cursoselecionado"]).Id;
                trace.Trace("guidCurso: " + guidCurso);
                if (entityAlunoXCursoDisponivel.Attributes.Contains("dio_periodo"))
                {
                    trace.Trace("Período: " + ((OptionSetValue)entityAlunoXCursoDisponivel["dio_periodo"]).Value);
                    if (((OptionSetValue)entityAlunoXCursoDisponivel["dio_periodo"]).Value == 914300000)
                    {
                        PeriodoCurso = "Diurno";
                        trace.Trace("Período Diurno");
                    }
                    else
                    {
                        PeriodoCurso = "Noturno";
                        trace.Trace("Período Noturno");
                    }
                }
                if (entityAlunoXCursoDisponivel.Attributes.Contains("dio_datadeinicio"))
                {
                    DateTime varDataInicio = ((DateTime)entityAlunoXCursoDisponivel["dio_datadeinicio"]);
                    dataInicio = new DateTime(varDataInicio.Year, varDataInicio.Month, varDataInicio.Day);
                    trace.Trace("dataInicio: " + dataInicio);
                    trace.Trace("Dia da Semana: " + dataInicio.ToString("ddd"));
                }
            }
            if (guidCurso != Guid.Empty)
            {
                Entity entityCurso = service.Retrieve("curso_cursosdisponiveis", guidCurso, new ColumnSet("dio_duracao"));
                int horasDuracao = 0;
                if (entityCurso != null && entityCurso.Attributes.Contains("dio_duracao"))
                {
                    horasDuracao = Convert.ToInt32(entityCurso.Attributes["dio_duracao"].ToString());
                }
                trace.Trace("horasDuracao: " + horasDuracao);

                int diasNecessarios = 0;
                if (PeriodoCurso == "Diurno")
                {
                    diasNecessarios = horasDuracao / 8;
                    trace.Trace("diasNecessarios: " + diasNecessarios);
                }
                else if (PeriodoCurso == "Noturno")
                {
                    diasNecessarios = horasDuracao / 4;
                    trace.Trace("diasNecessarios: " + diasNecessarios);
                }

                // Cria o calendário do aluno
                if (diasNecessarios > 0)
                {
                    for (int i = 0; i < diasNecessarios; i++)
                    {
                        if (dataInicio.ToString("ddd") == "Sat" && PeriodoCurso == "Noturno")
                        {
                            dataInicio = dataInicio.AddDays(2);
                        }
                        // cria entidade de calendário
                        Entity entCalendarioAluno = new Entity("dio_calendariodoaluno");
                        entCalendarioAluno["dio_name"] = "Aula do dia " + dataInicio.ToString("ddd") + " - " + dataInicio;
                        entCalendarioAluno["dio_data"] = dataInicio;
                        entCalendarioAluno["dio_alunoxcursodisponivel"] = new EntityReference("curso_alunoxcursodisponvel", guidAlunoXCurso);
                        service.Create(entCalendarioAluno);

                        trace.Trace("Aula: " + i.ToString() + " - Data: " + dataInicio);

                        dataInicio = dataInicio.AddDays(1);
                    }
                }
            }
        }
    }
}
