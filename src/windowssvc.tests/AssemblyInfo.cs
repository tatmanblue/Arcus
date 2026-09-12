using Xunit;

// Several test classes here mutate process-wide environment variables (ARCUS_* config)
// around each test and restore them afterward -- safe as long as no two such classes run
// concurrently. xUnit parallelizes across test classes by default, which allowed a real,
// reproducible race between StreamCipherFactoryTests and LocalDataAccessEncryptionTests
// (both touch ARCUS_ENCRYPTION_ALGORITHM). Disabling parallelization for this assembly
// is simpler and more robust than herding every current and future env-var-touching class
// into one shared collection -- the whole suite runs in well under a second either way.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
