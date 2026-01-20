namespace FindIFBot.Domain
{
    public enum UserState
    {
        Idle,
        WaitingForFindQuery,
        WaitingForAdContent,
        WaitingForAdvice,
        ConfirmFindContent,
        ConfirmAdContent
    }
}
