using Gateway.Domain.Events;
using MassTransit;

namespace Gateway.Application.Sagas;

public class PaymentStateMachine : MassTransitStateMachine<PaymentState>
{
    public State Processing { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }

    public Event<PaymentApprovedEvent> PaymentApproved { get; private set; }
    public Event<SettlementCompletedEvent> SettlementCompleted { get; private set; }
    public Event<SettlementFailedEvent> SettlementFailed { get; private set; }

    public PaymentStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => PaymentApproved, x => x.CorrelateById(m => m.Message.TransactionId));
        Event(() => SettlementCompleted, x => x.CorrelateById(m => m.Message.TransactionId));   
        Event(() => SettlementFailed, x => x.CorrelateById(m => m.Message.TransactionId));

        Initially(
            When(PaymentApproved)
                .Then(context =>
                {
                    context.Saga.MerchantId = context.Message.MerchantId;
                    context.Saga.Amount = context.Message.Amount;
                    context.Saga.CreatedAt = context.Message.ApprovedAt;
                })
                .TransitionTo(Processing)
        );

        During(Processing,
            When(SettlementCompleted)
                .Then(context => context.Saga.UpdatedAt = context.Message.CompletedAt)
                .TransitionTo(Completed),

            When(SettlementFailed)
                .Then(context =>
                {
                    context.Saga.UpdatedAt = context.Message.FailedAt;
                })
                .Publish(context => new CancelPaymentMessage
                {
                    TransactionId = context.Saga.CorrelationId,
                    Reason = context.Message.Reason
                })
                .TransitionTo(Failed)
        ); 
    }
}