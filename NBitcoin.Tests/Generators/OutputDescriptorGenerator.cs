using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin.Altcoins;
using NBitcoin.Scripting;

#nullable enable
namespace NBitcoin.Tests.Generators
{
	public class OutputDescriptorGenerator : OutputDescriptorGeneratorBase
	{

		public static Arbitrary<OutputDescriptor> OutputDescriptorArb() =>
			Arb.From(OutputDescriptorGen());
	}

	public class RegtestOutputDescriptorGenerator : OutputDescriptorGeneratorBase
	{
		public static Arbitrary<OutputDescriptor> OutputDescriptorArb() =>
			Arb.From(OutputDescriptorGen(Network.RegTest));
	}

	public class OutputDescriptorGeneratorBase
	{
		public static Gen<OutputDescriptor> OutputDescriptorGen(Network? n = null) =>
			Gen.OneOf(
				AddrOutputDescriptorGen(n),
				RawOutputDescriptorGen(),
				PKOutputDescriptorGen(n),
				PKHOutputDescriptorGen(n),
				WPKHOutputDescriptorGen(n),
				ComboOutputDescriptorGen(n),
				MultisigOutputDescriptorGen(3, n), // top level multisig can not have more than 3 pubkeys.
				SHOutputDescriptorGen(n),
				WSHOutputDescriptorGen(n)
				);
		private static Gen<OutputDescriptor> AddrOutputDescriptorGen(Network? n = null) =>
			from addr in n is null ? AddressGenerator.RandomAddress() : AddressGenerator.RandomAddress(n)
			select OutputDescriptor.NewAddr(addr);

		private static Gen<OutputDescriptor> RawOutputDescriptorGen() =>
			from addr in ScriptGenerator.RandomScriptSig()
			where addr._Script.Length > 0
			select OutputDescriptor.NewRaw(addr);
		private static Gen<OutputDescriptor> PKOutputDescriptorGen(Network? n = null) =>
			from pkProvider in PubKeyProviderGen(n)
			select OutputDescriptor.NewPK(pkProvider);

		private static Gen<OutputDescriptor> PKHOutputDescriptorGen(Network? n = null) =>
			from pkProvider in PubKeyProviderGen(n)
			select OutputDescriptor.NewPKH(pkProvider);

		private static Gen<OutputDescriptor> WPKHOutputDescriptorGen(Network? n = null) =>
			from pkProvider in PubKeyProviderGen(n)
			select OutputDescriptor.NewWPKH(pkProvider);

		private static Gen<OutputDescriptor> ComboOutputDescriptorGen(Network? n = null) =>
			from pkProvider in PubKeyProviderGen(n)
			select OutputDescriptor.NewCombo(pkProvider);

		private static Gen<OutputDescriptor> MultisigOutputDescriptorGen(int maxN, Network? network = null) =>
			from n in Gen.Choose(2, maxN)
			from m in Gen.Choose(2, n).Select(i => (uint)i)
			from pkProviders in Gen.ArrayOf(n, PubKeyProviderGen(network))
			from isSorted in Arb.Generate<bool>()
			select OutputDescriptor.NewMulti(m, pkProviders, isSorted);

		private static Gen<OutputDescriptor> WSHInnerGen(int maxMultisigN, Network? n = null) =>
			Gen.OneOf(
				PKOutputDescriptorGen(n),
				PKHOutputDescriptorGen(n),
				MultisigOutputDescriptorGen(maxMultisigN, n)
				);
		private static Gen<OutputDescriptor> InnerOutputDescriptorGen(int maxMultisigN, Network? n = null) =>
			Gen.OneOf(
				WPKHOutputDescriptorGen(n),
				WSHInnerGen(maxMultisigN, n)
				);

		// For sh-nested script, max multisig Number is 15.
		private static Gen<OutputDescriptor> SHOutputDescriptorGen(Network? n = null) =>
			from inner in Gen.OneOf(InnerOutputDescriptorGen(15, n), WSHOutputDescriptorGen(n))
			select OutputDescriptor.NewSH(inner);

		private static Gen<OutputDescriptor> WSHOutputDescriptorGen(Network? n = null) =>
			from inner in WSHInnerGen(20, n)
			select OutputDescriptor.NewWSH(inner);

		#region pubkey providers

		private static Gen<PubKeyProvider> PubKeyProviderGen(Network? n = null) =>
			Gen.OneOf(OriginPubKeyProviderGen(n), ConstPubKeyProviderGen(), HDPubKeyProviderGen(n));

		private static Gen<PubKeyProvider> OriginPubKeyProviderGen(Network? n = null) =>
			from keyOrigin in CryptoGenerator.RootedKeyPath()
			from inner in Gen.OneOf(ConstPubKeyProviderGen(), HDPubKeyProviderGen(n))
			select PubKeyProvider.NewOrigin(keyOrigin, inner);

		private static Gen<PubKeyProvider> ConstPubKeyProviderGen() =>
			from pk in CryptoGenerator.PublicKey()
			select PubKeyProvider.NewConst(pk);

		private static Gen<PubKeyProvider> HDPubKeyProviderGen(Network? n = null) =>
			from extPk in n is null ? CryptoGenerator.BitcoinExtPubKey() : CryptoGenerator.ExtPubKey().Select(e => new BitcoinExtPubKey(e, n))
			from kp in CryptoGenerator.KeyPath()
			from t in Arb.Generate<PubKeyProvider.DeriveType>()
			select PubKeyProvider.NewHD(extPk, kp, t);

		# endregion
	}
}
#nullable disable
