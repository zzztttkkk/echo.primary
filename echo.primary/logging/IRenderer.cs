using System.Text;

namespace echo.primary.logging;

public interface IRenderer {
	string TimeLayout { get; }

	void Render(StringBuilder dst, LogItem log);
}