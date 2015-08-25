namespace VisualFSharpExample

open ApplebotAPI

[<PlatformRegistrar(typedefof<Platform>)>]
type VisualFSharpCommand() = 
    inherit Command("Visual FSharp Command")
    override this.HandleMessage<'T1, 'T2 when 'T1 :> Message and 'T2 :> Platform>(message : 'T1, sender : 'T2) = 
        Logger.Log("Response from Visual FSharp Command")