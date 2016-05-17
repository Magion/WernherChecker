using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WernherChecker
{
    public class PartSelection
    {
        public Vector2 startClick = -Vector2.one;
        public Rect selection;
        public List<Part> selectedParts = new List<Part>();
        Vector2 mousePos = Input.mousePosition;
        
        public void Update(Vector2 mousePos)
        {
            if (Input.GetMouseButtonDown(0))
            {
                startClick = mousePos;
            }

            else if (Input.GetMouseButtonUp(0))
            {
                selectedParts.Clear();
                foreach (Part part in WernherChecker.VesselParts)
                {
                    Vector3 origScreenPoint = EditorCamera.Instance.cam.WorldToScreenPoint(part.partTransform.position);
                    Vector3 correctedScreenPoint = new Vector3(origScreenPoint.x, Screen.height - origScreenPoint.y, origScreenPoint.z);
                    if (selection.Contains(correctedScreenPoint))
                    {
                        selectedParts.Add(part);
                    }
                }
                startClick = -Vector2.one;
            }

            if (Input.GetMouseButton(0) && !WernherChecker.Instance.mainWindow.Contains(mousePos))
            {
                selection = new Rect(startClick.x, startClick.y, mousePos.x - startClick.x, mousePos.y - startClick.y);

                if (selection.width < 0)
                {
                    selection.x = selection.x - Math.Abs(selection.width);
                    selection.width = Math.Abs(selection.width);
                }

                if (selection.height < 0)
                {
                    selection.y = selection.y - Math.Abs(selection.height);
                    selection.height = Math.Abs(selection.height);
                }

                GUI.DrawTexture(selection, (Texture)GameDatabase.Instance.GetTexture("WernherChecker/Data/selection", false));
                foreach (Part part in WernherChecker.VesselParts)
                {
                    Vector3 origScreenPoint = EditorCamera.Instance.cam.WorldToScreenPoint(part.partTransform.position);
                    Vector3 correctedScreenPoint = new Vector3(origScreenPoint.x, Screen.height - origScreenPoint.y, origScreenPoint.z);
                    if (selection.Contains(correctedScreenPoint))
                    {
                        part.SetHighlightType(Part.HighlightType.AlwaysOn);
                        part.SetHighlightColor(new Color(10f, 0.9f, 0f));
                    }
                    else
                        part.SetHighlightDefault();
                }
            }
        }

    }
}
