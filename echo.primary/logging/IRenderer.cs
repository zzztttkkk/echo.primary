using System.Text;

namespace echo.primary.logging;

public interface IRenderer {
	string TimeLayout { get; set; }

	void Render(StringBuilder dst, string name, LogItem log);
}