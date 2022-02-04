#### Should the analyzer suppose the method iterate the input parameter or not?

   I have tested using Asp.Net.Core and Roslyn solution. If the method is accepting IEnumerable<T>, then over 99% of them are finally iterate it using for each loop/linq method/string.Join.
   If the code is just checking for null, it would most likely accept object as the parameter type.
   So it is fine to suppose the method always iterate the parameter if it has 'IEnumerable' type.

#### How to extend it by using the editorConfig?

  EditorConfig could be used to let the user add the 'No Numerated method' list, like [CA1062](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1062)
  This would look like
  ```
  dotnet_code_quality.CA1851.no_enumeration_methods = M:NS.Bar.Method1|M:NS.Bar.Method2
  ```
  which tells the analyzer, method1 and method2 won't iterated its parameters. It won't let the user to specify a certain parameter won't be enumerated.
  Example:
  ```
  void Method1(IEnumerable<int> a, IEnumerable<int> b) {}
  ```
  If it is put in the list, then `a` and `b` are both not iterated.
  We could potentially provide this via editor config, but it would introduce many complexity in the config. (The format of C# and VB methods is already not easy for user to understand)
  So if we have to support it based on the feedback, then we should add attributes to support it. (i.e. [NoEnumeration] attribute)

  Questions: Should we also provide 'linq_chain_methods' in the editor config?