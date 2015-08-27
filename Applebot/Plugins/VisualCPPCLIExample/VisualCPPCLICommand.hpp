#pragma once

using namespace ApplebotAPI;

namespace VisualCPPCLIExample
{

	[PlatformRegistrar(Platform::typeid)]
	ref class VisualCPPCLICommand : public Command
	{
	public: 
		VisualCPPCLICommand();

		generic<class T1, class T2>
			void HandleMessage(T1 message, T2 platform) override;
	};

}