<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:tei="http://www.tei-c.org/ns/1.0" version="1.0">

    <!-- teiHeader -->
    <xsl:template match="tei:teiHeader"></xsl:template>

    <!-- head -->
    <xsl:template match="tei:head">
        <h2>
            <xsl:apply-templates />
        </h2>
    </xsl:template>

    <!-- div -->
    <xsl:template match="tei:div">
        <div>
            <xsl:if test="@type">
                <xsl:attribute name="class">
                    <xsl:value-of select="@type" />
                </xsl:attribute>
            </xsl:if>
            <xsl:apply-templates />
        </div>
    </xsl:template>

    <!-- lg -->
    <xsl:template match="tei:lg">
        <div class="lg">
            <xsl:apply-templates />
        </div>
    </xsl:template>

    <!-- l -->
    <xsl:template match="tei:l">
        <p class="l">
            <xsl:if test="@n">
                <span class="nr">
                    <xsl:value-of select="@n" />
                </span>
            </xsl:if>
            <xsl:apply-templates />
        </p>
    </xsl:template>

    <!-- geogName -->
    <xsl:template match="tei:geogName">
        <span class="geog-name" title="geographic name">
            <xsl:apply-templates />
        </span>
    </xsl:template>

    <!-- persName -->
    <xsl:template match="tei:persName">
        <span class="pers-name" title="person name">
            <xsl:apply-templates />
        </span>
    </xsl:template>

    <!-- quote -->
    <xsl:template match="tei:quote">
        <q>
            <xsl:apply-templates />
        </q>
    </xsl:template>

    <!-- hi -->
    <xsl:template match="tei:hi">
        <xsl:if test="@rend='hit'">
            <span class="hit">
                <xsl:apply-templates />
            </span>
        </xsl:if>
    </xsl:template>

    <!-- catch-all -->
    <xsl:template match="*">
        <xsl:message terminate="no">
            WARNING: Unmatched element:
            <xsl:value-of select="name()" />
        </xsl:message>
        <xsl:apply-templates />
    </xsl:template>

    <!-- root -->
    <xsl:template match="/tei:TEI">
        <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
        <xsl:variable name="title" select="/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title" />
        <html>
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <link rel="stylesheet" href="read.css" />
                <title>
                    <xsl:value-of select="$title" />
                </title>
            </head>
            <body>
                <article class="rendition">
                    <xsl:apply-templates />
                </article>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>