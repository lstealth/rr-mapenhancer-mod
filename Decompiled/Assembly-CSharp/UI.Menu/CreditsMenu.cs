using Markroader;
using UI.Builder;
using UI.Common;

namespace UI.Menu;

public class CreditsMenu : BuilderMenuBase
{
	protected override void BuildPanelContent(UIPanelBuilder builder)
	{
		string text = TMPMarkupRenderer.Render(Parser.Parse("\r\n<align=\"center\">\r\n# Credits\r\n\r\n### Creator/Lead Developer\r\nAdam Preble\r\n\r\n### Producer\r\nConnor Doornbos\r\n\r\n### Community Manager\r\nMatt \"Medmo\" Edmondson\r\n\r\n### Senior Technical Artist\r\nElijah Gooden\r\n\r\n### 3D Modelers\r\nKyle Gabba\r\nJeff Senaratne\r\nChance Clayton\r\nBen Kinser (Xyvoracle)\r\nMalcolm Kramp\r\nKrystopher Halamic\r\nLogan Goines\r\nMichael Humbert\r\nTyler Griffin\r\nVon Gruenheit\r\nCarlos Olivera\r\nJones Rana\r\nJames Scott\r\nAllen Entertainment\r\n\r\n### Sound\r\nGeorge Taylor\r\nChris Currao\r\nChristopher Tokarcik\r\n\r\n### Technical Advisors\r\nGeorge Taylor\r\nJoshua Anderchek\r\n\r\n### Testers & Consultants\r\nChance Clayton\r\nJoshua Anderchek\r\nNicholas F.\r\nRupert-James Littlewood <size=90%>(TFS)</size>\r\nMarcos Huizel\r\nChris Currao\r\nRudy Garbely\r\nCarolyn Hoffman\r\nSquiggie\r\nAnonymous\r\n\r\n### Special Thanks\r\nOur Families\r\nRick, Chris, Stuart\r\nNorm, Thomas, Ivan, Demetre\r\nBlender Foundation\r\n</align>\r\n"));
		builder.AddTextArea(text, delegate
		{
		}).FlexibleHeight();
		builder.Spacer(16f);
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.Spacer().FlexibleWidth(1f);
			uIPanelBuilder.AddButton("Back", delegate
			{
				this.NavigationController().Pop();
			});
			uIPanelBuilder.Spacer(22f);
			uIPanelBuilder.Spacer().FlexibleWidth(1f);
		});
		builder.Spacer(8f);
	}
}
