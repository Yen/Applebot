#include "VisualCPPCLICommand.hpp"

namespace VisualCPPCLIExample
{

	VisualCPPCLICommand::VisualCPPCLICommand()
		: Command("Visual CPP CLI Command")
	{}

	generic<class T1, class T2>
		void VisualCPPCLICommand::HandleMessage(T1 message, T2 sender)
		{
			Logger::Log("Response from Visual CPP CLI Command");
		}

}