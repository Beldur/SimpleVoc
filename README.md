A simple wrapper for SimpleVoc's REST interface in C#

	var _simpleVoc = new SimpleVocConnection("192.168.178.20", 8008);
		_simpleVoc.Set(new SimpleVocValue() { Key = "TestKey", Data = "Test data" });
	
	Console.WriteLine(_simpleVoc.Get("TestKey"));