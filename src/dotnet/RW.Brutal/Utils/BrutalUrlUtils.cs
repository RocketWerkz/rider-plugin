// Generated via `rocket plugin` command
// Last generated: 2025-09-10 14:49:52
using System;
namespace RW.Brutal;
public static class BrutalUrlUtils
{
	public static string GetPackageIdFromNamespace(in string ns)
	{
		if(
			ns.StartsWith("Brutal.Framework")
		)
			return "";
		if(
			ns == "Brutal"
		)
			return "http://core.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Fmod.Examples")
			|| ns.StartsWith("Brutal.FmodApi")
		)
			return "http://fmod.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.GlfwApi")
		)
			return "http://glfw.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.GliApi")
		)
			return "http://gli.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.ImGuiApi")
		)
			return "http://imgui.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.KtxApi")
		)
			return "http://ktx.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.OpenGlApi")
		)
			return "http://opengl.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.RakNetApi")
			|| ns.StartsWith("SwigTestApp")
			|| ns.StartsWith("InternalSwigTestApp")
		)
			return "http://raknet.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.StbApi")
		)
			return "http://stb.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.ShaderCompilerApi")
		)
			return "http://shadercompiler.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.TextureApi")
			|| ns.StartsWith("Brutal.Texture.Factory")
			|| ns.StartsWith("Brutal.GliApi")
			|| ns.StartsWith("Brutal.StbApi")
		)
			return "http://texture.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.NativeFileDialogApi")
		)
			return "http://nativefiledialog.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Box2DApi")
		)
			return "http://box2d.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Debug")
		)
			return "http://debug.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.VulkanApi")
		)
			return "http://vulkan.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.GltfApi")
		)
			return "http://gltf.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Interop")
		)
			return "http://interop.rocketwerkz.com/";

		throw new InvalidOperationException($"Unknown namespace {ns}");
	}
}
