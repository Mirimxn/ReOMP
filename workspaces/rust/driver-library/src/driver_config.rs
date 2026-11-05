#[derive(serde::Deserialize, serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DriverConfig
{
    pub engine_assembly_name: String,
    pub engine_assembly_path: String,
    pub engine_assembly_runtime_config_path: String,
    pub engine_class_name: String,
}
