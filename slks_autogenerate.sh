#!/usr/bin/env bash

rm -rfv Autogenerated

createRedirect() {
  package="$1"
  targetNamespace="$2"
  className="$3"
  targetPath="$4"
  
  mkdir -p "$(dirname "$targetPath")"
  
cat > "$targetPath" <<EOF
// This class is autogenerated! See slks_make_redirects.sh
namespace $targetNamespace {
  [System.Serializable]
  public class SLKS_$className : $package.$className {}
}
EOF
}

createPathRedirect() {
  package="$1"
  targetNamespace="$2"
  fragmentStart="$3"

  sourcePath="$(echo "$package" | sed "s_\._/_g")"
  
  for i in "$sourcePath"/*.cs; do
    echo "Creating proxy for $fragmentStart: $i"
    CLASS_NAME="$(basename "$i" | sed "s/\.cs//g")"
    echo " - Class: $CLASS_NAME"
    TARGET_PATH="Autogenerated/PartsProxy/${fragmentStart}Proxy_$CLASS_NAME.cs"
    echo " - Target path: $TARGET_PATH"
    createRedirect "$package" "$targetNamespace" "$CLASS_NAME" "$TARGET_PATH"
  done  
}

createPathRedirect "StarlitKaleidoscope.Parts.Generic" "XRL.World.Parts" "Part"
createPathRedirect "StarlitKaleidoscope.Parts.Mutations" "XRL.World.Parts.Mutation" "Mutation"

cat >Autogenerated/README.md <<EOF
# Autogenerated code for Starlit Kaleidoscope
The code here is autogenerated by \`slks_autogenerate.sh\`. Do not edit manually.
EOF
