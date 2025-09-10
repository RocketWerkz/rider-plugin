// Generated via `rocket plugin` command
// Last generated: 2025-09-10 13:58:15
using System;
namespace RW.Brutal;
public static class BrutalUrlUtils
{
	public static string GetPackageIdFromNamespace(in string ns)
	{
		if(
			ns.StartsWith("Brutal.Framework")
			|| ns.StartsWith("Brutal.Framework.Sandbox")
		)
			return "";
		if(
			ns.StartsWith("Brutal.Core")
		)
			return "http://core.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.FmodApi")
			|| ns.StartsWith("Brutal.Fmod.Generator")
		)
			return "http://fmod.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.GlfwApi")
			|| ns.StartsWith("Brutal.Glfw.Generate")
		)
			return "http://glfw.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.GliApi")
		)
			return "http://gli.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.ImGuiApi")
			|| ns.StartsWith("Brutal.ImGui.Generator")
		)
			return "http://imgui.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.KtxApi")
		)
			return "http://ktx.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.OpenGlApi")
			|| ns.StartsWith("Brutal.OpenGl.Generate")
		)
			return "http://opengl.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.RakNetApi")
			|| ns.StartsWith("Brutal.RakNet.Generate")
		)
			return "http://raknet.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.StbApi")
		)
			return "http://stb.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.ShaderCompilerApi")
			|| ns.StartsWith("Brutal.ShaderCompiler.Generate")
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
			|| ns.StartsWith("Brutal.Box2D.Generate")
		)
			return "http://box2d.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Debug")
		)
			return "http://debug.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.VulkanApi")
			|| ns.StartsWith("Brutal.Vulkan.Generate")
		)
			return "http://vulkan.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Gltf.Generator")
			|| ns.StartsWith("Brutal")
		)
			return "http://gltf.rocketwerkz.com/";
		if(
			ns.StartsWith("Brutal.Interop")
			|| ns.StartsWith("Brutal.Interop.Generate")
		)
			return "http://interop.rocketwerkz.com/";

		throw new InvalidOperationException($"Unknown namespace {ns}");
	}
}
