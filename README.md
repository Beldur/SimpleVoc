A simple wrapper for SimpleVoc's REST interface in C#.
Uses [Visual Studio Async CTP](http://msdn.microsoft.com/en-us/vstudio/gg316360).

	var _simpleVoc = new SimpleVocConnection("192.168.178.20", 8008);
	
	// Set
	_simpleVoc.Set(new SimpleVocValue()
	{
		Key = "User/1/Name",
		Data = "Peter Pan",
		Extended = new Dictionary<string, string>()
		{
			{ "type", "mischievous"}
		},
		Expires = DateTime.Now.AddDays(1)
	});
	
	// Get keys
	var user = _simpleVoc.GetKeys("User", "type==\"mischievous\"");

	// Get
	var result = _simpleVoc.Get("User/1/Name");
	
For asynchronous access you can use SetAsync, GetAsync and GetKeysAsync wich returns a of `Task<T>`.