using Conjecture.Core;

// MaxExamples = 200 for PR CI (ADR-0005); UseDatabase persists failing IRs to
// .conjecture/examples/ for automatic regression prevention.
[assembly: ConjectureSettings(MaxExamples = 200, UseDatabase = true)]
