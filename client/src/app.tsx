import { useEffect, useState } from "react";

function App() {
  const [message, setMessage] = useState<string>("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/hello")
      .then((res) => res.json())
      .then((data: { message: string }) => setMessage(data.message))
      .catch(() => setMessage("Could not reach backend — is it running?"))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="text-center max-w-md w-full">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">
          Welcome to QuattroLingo
        </h1>
        <p className="text-gray-500 mb-8">Login / Register</p>

        {/* Backend ping card */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
          <p className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-2">
            Backend test
          </p>
          {loading ? (
            <p className="text-gray-400 animate-pulse">Connecting…</p>
          ) : (
            <p className="text-lg font-semibold text-indigo-600">{message}</p>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
