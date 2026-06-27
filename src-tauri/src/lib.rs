use tauri_plugin_shell::ShellExt;
use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
  tauri::Builder::default()
    .plugin(tauri_plugin_shell::init())
    .setup(|app| {
      if cfg!(debug_assertions) {
        app.handle().plugin(
          tauri_plugin_log::Builder::default()
            .level(log::LevelFilter::Info)
            .build(),
        )?;
      }
      
      let resource_path = app.path().resolve("..\\.next\\standalone\\startup.js", tauri::path::BaseDirectory::Resource).unwrap();
      
      let sidecar_command = app.shell()
        .sidecar("node")
        .unwrap()
        .args([resource_path.to_str().unwrap()]);
        
      let (mut rx, mut _child) = sidecar_command.spawn().expect("Failed to spawn sidecar");

      tauri::async_runtime::spawn(async move {
        while let Some(event) = rx.recv().await {
            match event {
                tauri_plugin_shell::process::CommandEvent::Stdout(line) => {
                    println!("Node: {:?}", String::from_utf8(line));
                }
                tauri_plugin_shell::process::CommandEvent::Stderr(line) => {
                    eprintln!("Node Err: {:?}", String::from_utf8(line));
                }
                _ => {}
            }
        }
      });
      Ok(())
    })
    .run(tauri::generate_context!())
    .expect("error while running tauri application");
}
