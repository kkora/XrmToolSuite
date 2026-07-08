using Xunit;

// The UI E2E walkthroughs all drive the SAME single XrmToolBox instance, so they cannot run concurrently —
// parallel execution makes each test's screenshots capture another test's tool tab. Force xunit to run every
// test collection (i.e. every test class) sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
